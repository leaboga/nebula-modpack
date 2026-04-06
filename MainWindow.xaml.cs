using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installer;
using CmlLib.Core.ModLoaders.FabricMC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Documents;
using NebulaLauncher.Modules;
using NebulaLauncher.Services;

namespace NebulaLauncher
{

    public partial class MainWindow : Window
    {
        // ── Paths ─────────────────────────────────────────────────────────
        public MinecraftProfile? CurrentProfile => _session.Profiles.Find(p => p.Id == _session.CurrentProfileId) ?? (_session.Profiles.Count > 0 ? _session.Profiles[0] : null);
        private static readonly string AppFolder   = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher");
        private static readonly string SessionFile = System.IO.Path.Combine(AppFolder, "session.json");
        public string GameFolder => System.IO.Path.Combine(AppFolder, "instances", CurrentProfile?.Id ?? "default");
        private static readonly string LogFile     = System.IO.Path.Combine(AppFolder, "launcher.log");

        // ── Theme brushes ─────────────────────────────────────────────────
        private static readonly SolidColorBrush BrushOnline  = new(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly SolidColorBrush BrushOffline = new(Color.FromRgb(0xEF, 0x44, 0x44));

        private const string CurrentLauncherVersion = "1.8.4";
        private const string UpdateCheckUrl = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/version.json";
        
        // ── Services ──────────────────────────────────────────────────────
        private readonly SocialService          _socialService    = new();
        private readonly ServerStatusCache      _cache            = new();
        private readonly ChangelogService       _changelogService = new();
        private readonly SkinService            _skinService      = new();
        private readonly SessionHistoryService  _historyService   = new();
        private readonly DiscordRPCService      _discord          = new();
        private BackupService                   _backupService    = null!;
        private CrashReporterService            _crashReporter    = null!;
        private ModSyncer                       _syncer           = null!;

        // ── State ─────────────────────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════
        //  WINDOW CONTROLS
        // ══════════════════════════════════════════════════════════════════
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ResizeMode != ResizeMode.NoResize)
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        public UserSession    Session          => _session;
        private UserSession   _session         = new();
        private ModManifest?  _manifestActual;
        private VersionsIndex? _versionsIndex;
        private bool          _cerrarDeVerdad  = false;
        private bool          _isInitializing  = false;
        private DispatcherTimer _updateTimer   = null!;
        private readonly System.Windows.Media.MediaPlayer _bgPlayer = new();
        private bool _isMusicPlaying = false;

        // ── Particles ─────────────────────────────────────────────────────
        private readonly List<(Ellipse dot, double vx, double vy)> _particles = new();
        private DispatcherTimer? _particleTimer;
        private readonly Random _rnd = new();

        // ── Notifications (friend tracking) ───────────────────────────────
        private HashSet<string> _lastOnlinePlayers = new();
        private DispatcherTimer _perfTimer = null!;
        private readonly PerformanceCounter? _cpuCounter; // Optional: Only if available 
        private Process? _gameProcess;

        public MainWindow()
        {
            InitializeComponent();

            Directory.CreateDirectory(AppFolder);
            CargarSesion();

            InitializeProfileServices();

            PlayButton.IsEnabled = false;

            ActualizarColorTema();
            ActualizarFondo();
            IniciarParticulas();
            
            _discord.Initialize();
            _discord.SetIdle();
            IniciarUpdateTimer();

            _perfTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _perfTimer.Tick += (s, e) => ActualizarMonitores();
            _perfTimer.Start();

            // MODERN UI EFFECTS (Win11+)
            ModernUIHelper.SetDarkTitleBar(this);
            
            this.Loaded += (s, e) => {
                ActualizarFondo();
                ActualizarGreeting();
                var sb = (Storyboard)FindResource("FadeIn");
                sb.Begin(MainRoot);
                
                var pulse = (Storyboard)FindResource("PulseEffect");
                pulse.Begin(LiveNewsBadge);
                pulse.Begin(ActiveUserDot);
            };

            this.SourceInitialized += (s, e) =>
            {
                ModernUIHelper.ApplyMica(this);
                ModernUIHelper.SetDarkTitleBar(this);
            };

            this.MouseDown += MainWindow_MouseDown;

            Task.Run(async () =>
            {
                await CargarVersionesAsync();
                await UpdateServerStatus();
                
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
                
                Dispatcher.Invoke(() => {
                    VersionFooterLabel.Text = $"Nebula Launcher v{currentVersion}";
                    AgregarLog($"🛡️ Version v{currentVersion} — Sistema operativo cargado con éxito.");
                    
                    // Auto-focus para piloto en offline
                    if (_session.AuthMode == "offline" && string.IsNullOrEmpty(_session.Username))
                        NickTextBox.Focus();
                });
                
                // Diferir update check para no quitar prioridad al juego
                await Task.Delay(2000);
                await CheckForLauncherUpdate();
                ActualizarSessionHistoryUI();
                await RefrescarSkin();
                
                Dispatcher.Invoke(() => {
                    ActualizarComboPerfiles();
                });
            });

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _updateTimer.Tick += (s, e) => { _ = UpdateServerStatus(); };
            TrayToggle.IsChecked = _session.MinimizeToTray;
            TurboToggle.IsChecked = _session.IsTurboEnabled;
            _updateTimer.Start();
        }

        // ══════════════════════════════════════════════════════════════════
        //  PARTICLES
        // ══════════════════════════════════════════════════════════════════
        private void IniciarParticulas()
        {
            for (int i = 0; i < 45; i++)
            {
                double size    = _rnd.NextDouble() * 3 + 1;
                double opacity = _rnd.NextDouble() * 0.35 + 0.05;
                var dot = new Ellipse
                {
                    Width  = size,
                    Height = size,
                    Fill   = new SolidColorBrush(Color.FromArgb(
                        (byte)(opacity * 255),
                        (byte)_rnd.Next(100, 200),
                        (byte)_rnd.Next(50, 150),
                        (byte)_rnd.Next(200, 255)))
                };
                double x = _rnd.NextDouble() * 1020;
                double y = _rnd.NextDouble() * 660;
                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot,  y);
                ParticleCanvas.Children.Add(dot);

                double speed = _rnd.NextDouble() * 0.3 + 0.05;
                double angle = _rnd.NextDouble() * Math.PI * 2;
                _particles.Add((dot, Math.Cos(angle) * speed, Math.Sin(angle) * speed));
            }

            _particleTimer = null; // No longer using timer
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            MoverParticulas();
        }

        private void MoverParticulas()
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            for (int i = 0; i < _particles.Count; i++)
            {
                var (dot, vx, vy) = _particles[i];
                double x = Canvas.GetLeft(dot) + vx;
                double y = Canvas.GetTop(dot)  + vy;

                if (x < -10) x = w + 10;
                else if (x > w + 10) x = -10;
                
                if (y < -10) y = h + 10;
                else if (y > h + 10) y = -10;

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SERVER STATUS + FRIEND NOTIFICATIONS
        // ══════════════════════════════════════════════════════════════════
        private async Task UpdateServerStatus()
        {
            ServerInfo? status = null;
            try { status = await _socialService.GetServerStatus(_session.ServerIp); } catch { }

            Dispatcher.Invoke(() =>
            {
                bool online = status?.IsOnline == true;
                
                if (online)
                {
                    _cache.Save(status!);
                    StatusDot.Fill  = BrushOnline;
                    StatusText.Text  = "Online";
                    PlayersText.Text = $"{status!.OnlinePlayers} / {status.MaxPlayers} online";
                    _discord.SetPresence("En el launcher", $"{status.OnlinePlayers} jugadores online");
                }
                else
                {
                    // Check cache
                    var cached = _cache.Load();
                    if (cached.HasData)
                    {
                        string timeLabel = _cache.GetLastSeenLabel(cached.LastSeen);
                        StatusText.Text = $"Offline ({timeLabel})";
                        PlayersText.Text = $"Último: {cached.Status.OnlinePlayers} jug.";
                    }
                    else
                    {
                        StatusText.Text = "Cerrado";
                        PlayersText.Text = "";
                    }
                    StatusDot.Fill = BrushOffline;
                    _discord.SetPresence("En el launcher", "Servidor offline");
                }

                StatusDot.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color       = online ? Color.FromRgb(0x10, 0xB9, 0x81) : Color.FromRgb(0xEF, 0x44, 0x44),
                    Opacity     = 0.9, BlurRadius = 8, ShadowDepth = 0
                };
            });

            // Friend notifications
            CheckFriendNotifications(status);
        }

        private void CheckFriendNotifications(ServerInfo? status)
        {
            if (status?.Players == null) return;
            
            var currentPlayers = new HashSet<string>(status.Players);
            foreach (var p in currentPlayers)
            {
                if (!_lastOnlinePlayers.Contains(p))
                {
                    AgregarLog($"👋 {p} se ha unido al servidor.");
                    // Mejora: Toast feedback visual rápido
                    Dispatcher.Invoke(() => {
                        StatusText.Text = $"✨ {p} entró!";
                        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                        timer.Tick += (s, e) => { StatusText.Text = "Online"; timer.Stop(); };
                        timer.Start();
                    });
                }
            }
            _lastOnlinePlayers = currentPlayers;
        }

        public void ActualizarFondo()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_session.BackgroundImagePath) && System.IO.File.Exists(_session.BackgroundImagePath)) {
                    var uri = new Uri(_session.BackgroundImagePath);
                    var bmp = new BitmapImage();
                    bmp.BeginInit(); bmp.UriSource = uri; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit();
                    LauncherBackground.Source = bmp; return;
                }
                
                // Dynamic System Backgrounds (Based on Hour) - Improvement 13
                int hour = DateTime.Now.Hour;
                string nebulaUrl = "https://images.unsplash.com/photo-1462331940025-496dfbfc7564?q=80&w=1000"; // Default: Night
                if (hour >= 6 && hour < 12)  nebulaUrl = "https://images.unsplash.com/photo-1444703686981-a3abbc4d4fe3?q=80&w=1000"; // Morning
                if (hour >= 12 && hour < 19) nebulaUrl = "https://images.unsplash.com/photo-1475274047050-1d0c0975c63e?q=80&w=1000"; // Evening
                
                var img = new BitmapImage(new Uri(nebulaUrl));
                LauncherBackground.Source = img;
                LauncherBackground.Opacity = 0.15;
            }
            catch { LauncherBackground.Source = null; }
        }

        public void ActualizarColorTema()
        {
            try
            {
                if (string.IsNullOrEmpty(_session.AccentColor)) return;
                var color = (Color)ColorConverter.ConvertFromString(_session.AccentColor);
                
                Application.Current.Resources["AccentColor"]      = color;
                Application.Current.Resources["AccentBrush"]      = new SolidColorBrush(color);
                Application.Current.Resources["GlowColor"]        = color;
                
                var hoverColor = Color.FromArgb(color.A, 
                    (byte)Math.Min(255, color.R + 30), 
                    (byte)Math.Min(255, color.G + 30), 
                    (byte)Math.Min(255, color.B + 30));
                Application.Current.Resources["AccentHoverColor"] = hoverColor;

                if (AvatarInitial != null) AvatarInitial.Foreground = new SolidColorBrush(color);
                if (PercentageLabel != null) PercentageLabel.Foreground = new SolidColorBrush(color);
                if (InstalledVersionText != null) InstalledVersionText.Foreground = new SolidColorBrush(color);
            }
            catch (Exception ex) { AgregarLog($"⚠ Error aplicando tema: {ex.Message}"); }
        }

        // ══════════════════════════════════════════════════════════════════
        //  LAUNCHER UPDATE CHECK
        // ══════════════════════════════════════════════════════════════════
        private string? _updateDownloadUrl;
        private string? _updateVersion;

        private async Task CheckForLauncherUpdate()
        {
            try
            {
                using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher");
                
                var response = await http.GetStringAsync(UpdateCheckUrl);
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                if (root == null) return;

                string latest = root.version?.ToString() ?? "";
                if (string.IsNullOrEmpty(latest) || latest == CurrentLauncherVersion) 
                {
                    AgregarLog("Nebula Launcher esta actualizado.");
                    return;
                }

                string changelog = root.changelog?.ToString() ?? "";
                _updateDownloadUrl = root.download_url?.ToString();
                _updateVersion = latest;

                Dispatcher.Invoke(() =>
                {
                    UpdateBadge.Text       = "v" + latest + " disponible";
                    UpdateBadge.Visibility = Visibility.Visible;
                    UpdateBadge.ToolTip    = changelog;
                    AgregarLog("Actualizacion disponible: v" + latest);
                });
            }
            catch (Exception ex) { AgregarLog("Error verificando actualizaciones: " + ex.Message); }
        }

        private async void UpdateBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_updateVersion)) return;

            var result = MessageBox.Show(
                "Nueva version disponible: v" + _updateVersion + "\n\n" +
                UpdateBadge.ToolTip + "\n\n" +
                "El launcher se descargara y reiniciara. \u00BFContinuar?",
                "Actualizacion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

            UpdateBadge.Text      = "Descargando...";
            UpdateBadge.IsEnabled = false;

            try
            {
                if (string.IsNullOrEmpty(_updateDownloadUrl))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/leaboga/nebula-modpack/releases/latest")
                        { UseShellExecute = true });
                    return;
                }
                await AplicarUpdateAsync(_updateDownloadUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateBadge.Text      = "v" + _updateVersion + " disponible";
                UpdateBadge.IsEnabled = true;
            }
        }

        private async Task AplicarUpdateAsync(string downloadUrl)
        {
            try
            {
                string currentExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(currentExe))
                    currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                string tempExe = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NebulaLauncher_new.exe");
                string updaterBat = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nebula_updater.bat");

                AgregarLog("Descargando actualizacion...");

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher");
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(tempExe, bytes);

                string batContent = "@echo off\n" +
                                   "ping -n 3 127.0.0.1 > nul\n" +
                                   "copy /Y \"" + tempExe + "\" \"" + currentExe + "\"\n" +
                                   "del \"" + tempExe + "\"\n" +
                                   "start \"\" \"" + currentExe + "\"\n" +
                                   "del \"%~f0\"\n";

                await File.WriteAllTextAsync(updaterBat, batContent);

                AgregarLog("\uD83D\uDD04 Reiniciando para aplicar la actualización...");

                Process.Start(new ProcessStartInfo("cmd.exe", "/C \"" + updaterBat + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                _cerrarDeVerdad = true;
                Dispatcher.Invoke(() => Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al aplicar actualización: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateBadge.IsEnabled = true;
            }
        }

        // Checker periódico (cada 1 hora)
        private void IniciarUpdateTimer()
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            t.Tick += (_, _) => _ = CheckForLauncherUpdate();
            t.Start();
        }

        // ══════════════════════════════════════════════════════════════════
        //  SKIN
        // ══════════════════════════════════════════════════════════════════
        private async Task RefrescarSkin()
        {
            if (string.IsNullOrWhiteSpace(_session.Username)) return;
            try
            {
                var bmp = await _skinService.GetSkinHeadAsync(_session.Username);
                Dispatcher.Invoke(() =>
                {
                    if (bmp != null)
                    {
                        SkinImage.Source     = bmp;
                        SkinImage.Visibility  = Visibility.Visible;
                        AvatarInitial.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        SkinImage.Visibility  = Visibility.Collapsed;
                        AvatarInitial.Visibility = Visibility.Visible;
                    }
                });
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        //  SESSION HISTORY
        // ══════════════════════════════════════════════════════════════════

        private void MusicToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isMusicPlaying)
                {
                    // Lofi-loop stream URL (Royalty Free)
                    _bgPlayer.Open(new Uri("https://stream.zeno.fm/f3dfu663ca0uv", UriKind.Absolute));
                    _bgPlayer.Volume = 0.3;
                    _bgPlayer.Play();
                    MusicToggle.Opacity = 1.0;
                    _isMusicPlaying = true;
                }
                else
                {
                    _bgPlayer.Stop();
                    MusicToggle.Opacity = 0.4;
                    _isMusicPlaying = false;
                }
            }
            catch { }
        }

        public async Task ForceUpdateStatus() => await UpdateServerStatus();

        private void ActualizarSessionHistoryUI()
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var history = _historyService.Load();
                    TotalTimeLabel.Text    = _historyService.FormatTotalTime(history.TotalMinutes);
                    SessionCountLabel.Text = $"{history.SessionCount} sesiones";
                }
                catch { }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  NAVIGATION
        // ══════════════════════════════════════════════════════════════════
        private void Nav_Home_Checked(object sender, RoutedEventArgs e)       { CambiarVista("home"); }
        private void Nav_Changelog_Checked(object sender, RoutedEventArgs e)  { CambiarVista("changelog"); }
        private void Nav_Settings_Checked(object sender, RoutedEventArgs e)   { CambiarVista("settings"); }
        private void Nav_Social_Checked(object sender, RoutedEventArgs e)     { CambiarVista("social"); }
        private void Nav_Perf_Checked(object sender, RoutedEventArgs e)       { CambiarVista("perf"); }
        private void Nav_Screenshots_Checked(object sender, RoutedEventArgs e) { CambiarVista("screenshots"); }
        private void Nav_ModManager_Checked(object sender, RoutedEventArgs e)  { CambiarVista("modmanager"); }
        private void Nav_ModHub_Checked(object sender, RoutedEventArgs e)      { CambiarVista("modhub"); }
        private void Nav_Crash_Checked(object sender, RoutedEventArgs e)       { CambiarVista("crash"); }
        private void Nav_BlueMap_Checked(object sender, RoutedEventArgs e)     { CambiarVista("map"); }
        
        private void MapQuickCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                NavBlueMap.IsChecked = true; // Al activar el radio button, se llama a Nav_BlueMap_Checked
            }
        }

        private void StopCurrentModule()
        {
            try
            {
                if (ModulesContainer?.Content is SocialView      sv) sv.Stop();
                if (ModulesContainer?.Content is PerformanceView pv) pv.Stop();
            }
            catch { }
        }

        private void SwitchToModule(UserControl module)
        {
            if (ModulesContainer == null) return;
            ModulesContainer.Visibility = Visibility.Visible;
            ModulesContainer.Content = module;
            AnimateView(ModulesContainer);
        }

        // ══════════════════════════════════════════════════════════════════
        //  COPY IP
        // ══════════════════════════════════════════════════════════════════
        private void CopyIpBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_session.ServerIp);
                var btn = (Button)sender;
                string orig  = btn.Content.ToString()!;
                btn.Content  = "\u2713 Copiado!";
                var timer    = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick  += (_, _) => { btn.Content = orig; timer.Stop(); };
                timer.Start();
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        //  BACKUP
        // ══════════════════════════════════════════════════════════════════
        private async void BackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn       = (Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "\u23F3 Creando backup...";
            try
            {
                string path = await _backupService.CreateBackupAsync(msg => AgregarLog(msg));
                MessageBox.Show($"Backup creado exitosamente:\n{System.IO.Path.GetFileName(path)}",
                                "Backup completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"\u274C Error en backup: {ex.Message}"); }
            finally { btn.IsEnabled = true; btn.Content = "\uD83D\uDCBE  Crear Backup Ahora"; }
        }

        // ══════════════════════════════════════════════════════════════════
        //  LOG
        // ══════════════════════════════════════════════════════════════════
        private void MainProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (PercentageLabel == null) return;
            int pct = (int)e.NewValue;
            PercentageLabel.Text = pct > 0 ? $"{pct}%" : "";
        }

        public void AgregarLog(string mensaje)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string time = $"[{DateTime.Now:HH:mm:ss}] ";
                    var runTime = new Run(time) { Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x42, 0x66)) };
                    var runText = new Run(mensaje);

                    // Syntax Highlighting simple
                    if (mensaje.StartsWith("\u2705") || mensaje.StartsWith("\u2713")) runText.Foreground = Brushes.LightGreen;
                    else if (mensaje.StartsWith("\u274C") || mensaje.StartsWith("\u2717") || mensaje.Contains("Error")) runText.Foreground = Brushes.Salmon;
                    else if (mensaje.StartsWith("\u26A0") || mensaje.Contains("Warning")) runText.Foreground = Brushes.Gold;
                    else if (mensaje.StartsWith("\uD83D\uDE80") || mensaje.StartsWith("\u26A1")) runText.Foreground = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
                    else runText.Foreground = new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD));

                    if (LogText.Text == "[Nebula] System initialized. Waiting for command...") LogText.Inlines.Clear();
                    
                    LogText.Inlines.Add(runTime);
                    LogText.Inlines.Add(runText);
                    LogText.Inlines.Add(new LineBreak());

                    if (LogText.Inlines.Count > 100) LogText.Inlines.Remove(LogText.Inlines.FirstInline);
                    
                    LogScroll?.ScrollToEnd();
                }
                catch { }
            }), DispatcherPriority.Background);
            
            Task.Run(() => { try { File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss}] {mensaje}\n"); } catch { } });
        }

        // ══════════════════════════════════════════════════════════════════
        //  SESSION PERSISTENCE
        // ══════════════════════════════════════════════════════════════════
        private void CargarSesion()
        {
            _isInitializing = true;
            try
            {
                if (File.Exists(SessionFile))
                    _session = JsonConvert.DeserializeObject<UserSession>(File.ReadAllText(SessionFile)) ?? new UserSession();
            }
            catch (Exception ex) { AgregarLog($"\u26A0 Error cargando sesi\u00F3n: {ex.Message}"); }

            if (_session.Profiles == null) _session.Profiles = new List<MinecraftProfile>();
            if (_session.Profiles.Count == 0)
            {
                var defaultProfile = new MinecraftProfile { Name = "Nebula Default (1.20.1)", Version = "1.20.1", LoaderType = "vanilla" };
                _session.Profiles.Add(defaultProfile);
                _session.CurrentProfileId = defaultProfile.Id;
            }
            if (string.IsNullOrEmpty(_session.CurrentProfileId)) _session.CurrentProfileId = _session.Profiles[0].Id;

            _session.Profiles.ForEach(p => { if (p.RamGB < 2) p.RamGB = 4; });

            if (RamSlider   != null) RamSlider.Value      = CurrentProfile?.RamGB ?? 4;
            if (TrayToggle  != null) TrayToggle.IsChecked = _session.MinimizeToTray;
            if (NickTextBox != null) NickTextBox.Text      = _session.Username;

            if (_session.AuthMode == "premium" && PremiumToggle != null)
            {
                PremiumToggle.IsChecked = true;
                if (OfflinePanel != null) OfflinePanel.Visibility = Visibility.Collapsed;
                if (PremiumPanel != null) PremiumPanel.Visibility = Visibility.Visible;
                if (!string.IsNullOrEmpty(_session.Username)) MostrarUsuarioPremium(_session.Username);
            }

            _crashReporter = new CrashReporterService(GameFolder, _session.CrashWebhookUrl);
            ActualizarSidebar();
            ActualizarGreeting();
            if (SkipConfigSyncCheck != null) SkipConfigSyncCheck.IsChecked = _session.SkipConfigSync;
            _isInitializing = false;
        }

        private void SkipConfigSyncCheck_Click(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;
            _session.SkipConfigSync = SkipConfigSyncCheck.IsChecked ?? false;
            GuardarSesion();
            AgregarLog(_session.SkipConfigSync ? "🛠️ Modo Dev: Sincronización de configs desactivada." : "🛠️ Sincronización de configs activada.");
        }

        public void GuardarSesion()
        {
            if (_isInitializing) return;
            Task.Run(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        string json = JsonConvert.SerializeObject(_session, Formatting.Indented);
                        File.WriteAllText(SessionFile, json);
                        break;
                    }
                    catch (IOException) { Task.Delay(100).Wait(); }
                    catch (Exception ex) { Debug.WriteLine($"Error guardando sesi\u00F3n: {ex.Message}"); break; }
                }
            });
        }

        private void ActualizarSidebar()
        {
            bool hasUser = !string.IsNullOrEmpty(_session.Username);
            SidebarUsername.Text = hasUser ? _session.Username : "Sin sesi\u00F3n";
            AvatarInitial.Text   = hasUser ? _session.Username[0].ToString().ToUpper() : "?";
            SidebarAuthType.Text = _session.AuthMode == "premium" ? "\u2726 Premium" : "Sin cuenta";
        }

        private void ActualizarGreeting()
        {
            if (HomeGreetingLabel == null) return;
            int hour = DateTime.Now.Hour;
            string greeting = hour < 12 ? "Buenos d\u00EDas" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            
            // News System (Imp 18)
            string[] news = {
                "\u00A1Nueva actualizaci\u00F3n de Shaders disponible!",
                "Se han a\u00F1adido 5 nuevos mods de optimizaci\u00F3n.",
                "El servidor est\u00E1 en modo Evento: x2 de XP.",
                "Record\u00E1 hacer backup antes de grandes cambios.",
                "\u00A1Gracias por ser parte de Nebula!"
            };
            string currentNews = news[new Random().Next(news.Length)];

            HomeGreetingLabel.Text = !string.IsNullOrEmpty(_session.Username)
                ? $"{greeting}, {_session.Username} \uD83D\uDC4B\n\uD83D\uDCE2 {currentNews}"
                : "Listo para jugar";
        }

        private void MostrarUsuarioPremium(string username)
        {
            if (LoggedUsernameText != null) LoggedUsernameText.Text      = username;
            if (NotLoggedPanel     != null) NotLoggedPanel.Visibility    = Visibility.Collapsed;
            if (LoggedPanel        != null) LoggedPanel.Visibility       = Visibility.Visible;
            ActualizarSidebar();
        }

        // ══════════════════════════════════════════════════════════════════
        //  UI EVENTS
        // ══════════════════════════════════════════════════════════════════
        private void TrayToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_session == null || TrayToggle == null) return;
            _session.MinimizeToTray = TrayToggle.IsChecked ?? false;
            GuardarSesion();
        }

        private void TurboToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_session == null || TurboToggle == null) return;
            _session.IsTurboEnabled = TurboToggle.IsChecked ?? false;
            GuardarSesion();
        }

        private void AuthToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (OfflinePanel == null) return;
            bool premium = PremiumToggle?.IsChecked == true;
            OfflinePanel.Visibility = premium ? Visibility.Collapsed : Visibility.Visible;
            PremiumPanel.Visibility = premium ? Visibility.Visible   : Visibility.Collapsed;
            _session.AuthMode = premium ? "premium" : "offline";
            GuardarSesion();
            ActualizarSidebar();
        }

        private void NickTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string nick = NickTextBox.Text.Trim();
            string clean = Regex.Replace(nick, @"[^a-zA-Z0-9_]", "");
            if (clean != nick) { NickTextBox.Text = clean; NickTextBox.CaretIndex = clean.Length; }
            _session.Username = clean;
            GuardarSesion();
            ActualizarSidebar();
            ActualizarGreeting();
            if (!string.IsNullOrWhiteSpace(clean))
                _ = RefrescarSkin();
        }

        private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (RamLabel == null || CurrentProfile == null) return;
            CurrentProfile.RamGB = (int)e.NewValue;
            RamLabel.Text  = $"{CurrentProfile.RamGB} GB";
            GuardarSesion();
        }

        private void ActualizarComboPerfiles()
        {
            ProfileComboBox.SelectionChanged -= ProfileComboBox_SelectionChanged;
            ProfileComboBox.Items.Clear();
            foreach (var p in _session.Profiles) ProfileComboBox.Items.Add($"{p.Icon} {p.Name}");
            ProfileComboBox.SelectedIndex = _session.Profiles.FindIndex(p => p.Id == _session.CurrentProfileId);
            ProfileComboBox.SelectionChanged += ProfileComboBox_SelectionChanged;
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = ProfileComboBox.SelectedIndex;
            if (idx < 0 || idx >= _session.Profiles.Count) return;
            
            _session.CurrentProfileId = _session.Profiles[idx].Id;
            GuardarSesion();
            InitializeProfileServices();
            AgregarLog($"📂 Perfil cambiado a: {_session.Profiles[idx].Name}");
            
            // Sync UI state
            _manifestActual = null;
            _ = CargarVersionesAsync();
            ActualizarGreeting();
            ActualizarSidebar();
            
            Dispatcher.Invoke(() => {
                ActualizarVersionesEnHome();
            });

            // Sync RAM slider safely
            if (RamSlider != null)
            {
                RamSlider.ValueChanged -= RamSlider_ValueChanged;
                RamSlider.Value = CurrentProfile?.RamGB ?? 4;
                if (RamLabel != null) RamLabel.Text = $"{RamSlider.Value} GB";
                RamSlider.ValueChanged += RamSlider_ValueChanged;
            }

            // Reload active module to apply new GameFolder
            if (ModulesContainer?.Content is ModManagerView) SwitchToModule(new ModManagerView(GameFolder));
            else if (ModulesContainer?.Content is VaultView) SwitchToModule(new VaultView(GameFolder, CurrentProfile));
            else if (ModulesContainer?.Content is ScreenshotsView) SwitchToModule(new ScreenshotsView(GameFolder));
        }

        private void InitializeProfileServices()
        {
            Directory.CreateDirectory(GameFolder);
            _syncer = new ModSyncer(GameFolder);
            _backupService = new BackupService(GameFolder);
            _crashReporter = new CrashReporterService(GameFolder, _session.CrashWebhookUrl);

            _syncer.OnLog += msg => AgregarLog(msg);
            _syncer.OnProgress += pct => Dispatcher.Invoke(() => MainProgressBar.Value = pct);
            _syncer.OnProgressLabel += lbl => Dispatcher.Invoke(() => ProgressLabel.Text = lbl);
        }

        public void DeleteCurrentProfile()
        {
            if (CurrentProfile == null) return;
            
            var profileToDelete = CurrentProfile;
            _session.Profiles.Remove(profileToDelete);
            
            if (_session.Profiles.Count == 0)
            {
                // Create a default profile if none left
                var p = new MinecraftProfile { Name = "Default", Version = "1.20.1", LoaderType = "fabric" };
                _session.Profiles.Add(p);
                _session.CurrentProfileId = p.Id;
            }
            else
            {
                _session.CurrentProfileId = _session.Profiles[0].Id;
            }
            
            GuardarSesion();
            InitializeProfileServices();
            ActualizarComboPerfiles();
            ActualizarSidebar();
            
            // Switch back to home
            Dispatcher.Invoke(() => {
                CambiarVista("home");
            });
            
            AgregarLog($"🗑️ Perfil '{profileToDelete.Name}' eliminado.");
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            // Simple logic for PoC: add a new 1.20.1 Fabric instance
            string name = "Perfil " + (_session.Profiles.Count + 1);
            var p = new MinecraftProfile { Name = name, Version = "1.20.1", LoaderType = "fabric", Icon = "\u2B50" };
            _session.Profiles.Add(p);
            _session.CurrentProfileId = p.Id;
            GuardarSesion();
            ActualizarComboPerfiles();
            AgregarLog($"✅ Perfil '{name}' creado con éxito.");
        }

        private void CloneProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentProfile == null) return;
            string newName = CurrentProfile.Name + " (Copia)";
            var clone = new MinecraftProfile { 
                Name = newName, 
                Version = CurrentProfile.Version, 
                LoaderType = CurrentProfile.LoaderType,
                LoaderVersion = CurrentProfile.LoaderVersion,
                RamGB = CurrentProfile.RamGB,
                JavaPath = CurrentProfile.JavaPath,
                Icon = CurrentProfile.Icon
            };
            _session.Profiles.Add(clone);
            _session.CurrentProfileId = clone.Id;
            GuardarSesion();
            ActualizarComboPerfiles();
            AgregarLog($"✅ Perfil '{newName}' clonado con éxito.");
        }

        private void VerLog_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(LogFile)) { AgregarLog("ℹ️ No hay log guardado aún."); return; }
            try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{LogFile}\"", UseShellExecute = true }); }
            catch (Exception ex) { AgregarLog($"⚠️ Error abriendo log: {ex.Message}"); }
        }

        private async void RepararModpack_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) { btn.IsEnabled = false; btn.Content = "⏳ Reparando..."; }
            try
            {
                await SincronizarTodoAsync();
                AgregarLog("✅ Sincronización completada.");
                MessageBox.Show("Sincronización y reparación completada con éxito.", "Nebula Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"❌ Error en reparación: {ex.Message}"); }
            finally { if (btn != null) { btn.IsEnabled = true; btn.Content = "🛠️ Reparar Pack"; } }
        }

        public async Task SincronizarTodoAsync()
        {
            if (CurrentProfile == null) return;
            AgregarLog("🛠️ Iniciando sincronización total (GitHub)...");
            
            _manifestActual = null; // Force reload from server
            await CargarVersionesAsync();
            
            if (_manifestActual != null)
            {
                // 1. Sync MODS
                PlayButton.Content = "Sincronizando mods...";
                bool modsOk = await _syncer.SincronizarMods(_manifestActual);
                
                // 2. Sync CONFIGS/ASSETS
                PlayButton.Content = "Actualizando configs...";
                await _syncer.SincronizarConfigs();
                
                if (modsOk)
                {
                    CurrentProfile.LastSyncDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    CurrentProfile.LastSyncHash = _manifestActual.Version;
                    GuardarSesion();
                    AgregarLog($"✓ Perfil '{CurrentProfile.Name}' sincronizado correctamente.");
                }
            }
            else
            {
                AgregarLog("⚠ No se pudo obtener el manifiesto de GitHub.");
            }
            
            PlayButton.Content = "▶  JUGAR";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        { 
            if (!_cerrarDeVerdad) 
            { 
                e.Cancel = true; 
                Hide(); 
                // Liberar memoria al minimizar
                GC.Collect();
                GC.WaitForPendingFinalizers();
            } 
        }

        public void CerrarDefinitivo()
        {
            _updateTimer?.Stop();
            _particleTimer?.Stop();
            StopCurrentModule();
            _discord.Dispose();
            _cerrarDeVerdad = true;
            Close();
        }

        // ══════════════════════════════════════════════════════════════════
        //  VERSIONS
        // ══════════════════════════════════════════════════════════════════
        private async Task CargarVersionesAsync()
        {
            try
            {
                AgregarLog("\uD83D\uDD0D Verificando versiones disponibles...");
                _versionsIndex = await _syncer.ObtenerVersionsIndex();

                if (_versionsIndex?.AvailableVersions == null || _versionsIndex.AvailableVersions.Count == 0)
                { AgregarLog("\u26A0 No se pudieron cargar las versiones."); return; }

                int currentIdx = 0;
                Dispatcher.Invoke(() =>
                {
                    VersionComboBox.SelectionChanged -= VersionComboBox_SelectionChanged;
                    VersionComboBox.Items.Clear();
                    foreach (var v in _versionsIndex.AvailableVersions) VersionComboBox.Items.Add(v.Label);
                    int savedIdx = _versionsIndex.AvailableVersions.FindIndex(v => v.Version == (CurrentProfile?.LastVersion ?? ""));
                    VersionComboBox.SelectedIndex = savedIdx >= 0 ? savedIdx : 0;
                    currentIdx = VersionComboBox.SelectedIndex;
                    VersionComboBox.SelectionChanged += VersionComboBox_SelectionChanged;
                });

                await CargarManifest(currentIdx >= 0 ? currentIdx : 0);
            }
            catch (Exception ex) { AgregarLog($"\u26A0 Error cargando versiones: {ex.Message}"); }
        }

        private async void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = VersionComboBox.SelectedIndex;
            if (idx < 0) return;
            PlayButton.IsEnabled = false;
            await CargarManifest(idx);
        }

        private async Task CargarManifest(int idx)
        {
            if (_versionsIndex?.AvailableVersions == null || idx >= _versionsIndex.AvailableVersions.Count) return;
            var entry = _versionsIndex.AvailableVersions[idx];
            try
            {
                var manifest = await _syncer.ObtenerManifest(entry.ManifestUrl);
                Dispatcher.Invoke(() =>
                {
                    _manifestActual          = manifest;
                    if (CurrentProfile != null) {
                        CurrentProfile.LastVersion = entry.Version;
                        if (manifest != null) {
                            CurrentProfile.Version       = manifest.MinecraftVersion;
                            CurrentProfile.LoaderType    = manifest.Modloader;
                            CurrentProfile.LoaderVersion = manifest.ModloaderVersion;
                        }
                    }
                    GuardarSesion();
                    InstalledVersionText.Text = manifest?.Version ?? "Sin datos";
                    PlayButton.IsEnabled      = manifest != null;
                    if (manifest == null) AgregarLog($"\u26A0 No se pudo cargar manifest para {entry.Label}.");
                    else AgregarLog($"\u2713 Versi\u00F3n lista: {manifest.Version}");
                });
            }
            catch (Exception ex) { AgregarLog($"\u26A0 Error cargando manifest: {ex.Message}"); }
        }

        // ══════════════════════════════════════════════════════════════════
        //  AUTH
        // ══════════════════════════════════════════════════════════════════
        private async void MicrosoftLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false; btn.Content = "Iniciando sesi\u00F3n...";
            AgregarLog("\uD83D\uDD10 Abriendo autenticaci\u00F3n de Microsoft...");
            try
            {
                var handler = JELoginHandlerBuilder.BuildDefault();
                var session = await handler.Authenticate();
                if (session != null && !string.IsNullOrEmpty(session.Username))
                {
                    _session.Username = session.Username;
                    _session.AuthMode = "premium";
                    GuardarSesion();
                    MostrarUsuarioPremium(session.Username);
                    AgregarLog($"\u2705 Sesi\u00F3n iniciada como {session.Username}.");
                    await RefrescarSkin();
                }
                else AgregarLog("\u26A0 La autenticaci\u00F3n no devolvi\u00F3 sesi\u00F3n v\u00E1lida.");
            }
            catch (Exception ex)
            {
                AgregarLog($"❌ Error en login Microsoft: {ex.Message}");
                MessageBox.Show($"Error iniciando sesi\u00F3n:\n{ex.Message}", "Error de autenticaci\u00F3n", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { btn.IsEnabled = true; btn.Content = "Iniciar sesi\u00F3n con Microsoft"; }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _session.Username = ""; _session.AuthMode = "offline";
            GuardarSesion();
            if (LoggedPanel    != null) LoggedPanel.Visibility    = Visibility.Collapsed;
            if (NotLoggedPanel != null) NotLoggedPanel.Visibility = Visibility.Visible;
            if (OfflineToggle  != null) OfflineToggle.IsChecked   = true;
            SkinImage.Visibility     = Visibility.Collapsed;
            AvatarInitial.Visibility = Visibility.Visible;
            ActualizarSidebar();
            ActualizarGreeting();
            AgregarLog("\u2139 Sesi\u00F3n cerrada.");
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) 
            {
                try { DragMove(); } catch { }
            }
        }

        // Unified sync and repair system already implemented in SincronizarTodoAsync.

        // ══════════════════════════════════════════════════════════════════
        //  ADMIN
        // ══════════════════════════════════════════════════════════════════
        private void AdminAccessButton_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = _session.Username.ToLower() == "leandro" || _session.IsAdmin;
            if (isAdmin) { AdminPanel.Visibility = AdminPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; return; }

            var dialog = new Window { Title = "Acceso Admin", Width = 340, Height = 160, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, ResizeMode = ResizeMode.NoResize, Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x0B, 0x1A)), FontFamily = FontFamily };
            var panel  = new StackPanel { Margin = new Thickness(24) };
            var lbl    = new TextBlock  { Text = "Clave de administrador:", Foreground = new SolidColorBrush(Color.FromRgb(0x7B, 0x6F, 0xA0)), FontSize = 12, Margin = new Thickness(0, 0, 0, 8) };
            var tb     = new PasswordBox { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x15, 0x28)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x26, 0x48)), BorderThickness = new Thickness(1), Padding = new Thickness(10, 8, 10, 8), FontSize = 14 };
            var btn    = new Button     { Content = "Acceder", Margin = new Thickness(0, 12, 0, 0), Height = 38, Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x3A, 0xED)), Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            btn.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
            tb.KeyDown += (_, ke) => { if (ke.Key == Key.Enter) { dialog.DialogResult = true; dialog.Close(); } };
            panel.Children.Add(lbl); panel.Children.Add(tb); panel.Children.Add(btn);
            dialog.Content = panel;
            dialog.Loaded += (_, _) => tb.Focus();
            if (dialog.ShowDialog() == true && tb.Password == "1530") { _session.IsAdmin = true; GuardarSesion(); AdminPanel.Visibility = Visibility.Visible; AgregarLog("\u2705 Modo admin activado."); }
            else if (dialog.DialogResult == true) AgregarLog("\u26A0 Clave incorrecta.");
        }

        public void ReiniciarInstancia()
        {
            try
            {
                InitializeProfileServices();
                _manifestActual = null;
                AgregarLog($"\uD83D\uDCC2 Instancia sincronizada con el perfil activo.");
                _ = CargarVersionesAsync();
            }
            catch (Exception ex) { AgregarLog($"\u274C Error al cambiar instancia: {ex.Message}"); }
        }

        // ══════════════════════════════════════════════════════════════════
        //  PLAY
        // ══════════════════════════════════════════════════════════════════
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_session.Username))
            { AgregarLog("\u26A0 Ingres\u00E1 un nombre de usuario."); MessageBox.Show("Ingres\u00E1 un nombre primero.", "Sin usuario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (_session.Username.Length < 3) { AgregarLog("\u26A0 El nombre debe tener al menos 3 caracteres."); return; }
            if (CurrentProfile == null) { AgregarLog("\u26A0 No hay perfil seleccionado."); return; }

            ImportarConfigsDeMinecraftOriginal();
            PlayButton.IsEnabled = false;
            PlayButton.Content   = "Iniciando...";

            var sessionStart = DateTime.Now;

            try
            {
                bool turboMode = _session.IsTurboEnabled || Keyboard.IsKeyDown(Key.LeftShift);
                
                // BACKUP AUTOMATICO (SEGURIDAD PRIMERO)
                if (!turboMode)
                {
                    AgregarLog("💾 Creando backup de seguridad (rápido)...");
                    await _backupService.CreateQuickConfigBackupAsync();
                }

                if (turboMode) AgregarLog("⚡ Modo Turbo activado — omitiendo sincronización de archivos.");

                if (!turboMode && _manifestActual != null)
                {
                    PlayButton.Content = "Sincronizando mods...";
                    _discord.SetActivity("Sincronizando mods...");
                    bool modsOk = await _syncer.SincronizarMods(_manifestActual);
                    if (!modsOk) { AgregarLog("❌ Falló la descarga de mods."); return; }

                    if (!_session.SkipConfigSync)
                    {
                        PlayButton.Content = "Actualizando configs...";
                        await _syncer.SincronizarConfigs();
                    }
                    else
                    {
                        AgregarLog("🛠️ Modo Dev: Omitiendo sincronización de configs.");
                    }
                }
                PlayButton.Content = "Iniciando Minecraft...";
                _discord.SetActivity("Iniciando Minecraft...");
                MainProgressBar.Value = 0;
                ProgressLabel.Text    = "Iniciando Minecraft...";

                // Discord: in game
                int onlinePlayers = 0;
                try { var s = await _socialService.GetServerStatus(_session.ServerIp); onlinePlayers = s?.OnlinePlayers ?? 0; } catch { }
                _discord.SetInGame(_session.Username, onlinePlayers, 20);

                if (_session.MinimizeToTray) Hide(); else WindowState = WindowState.Minimized;

                int exitCode = await LanzarMinecraft(CurrentProfile);

                Show(); WindowState = WindowState.Normal; Activate();
                MainProgressBar.Value = 0;
                ProgressLabel.Text = exitCode == 0 ? "Sesión finalizada." : $"Minecraft cerró con código {exitCode}.";

                // Record session
                var duration = DateTime.Now - sessionStart;
                if (duration.TotalMinutes >= 1) { _historyService.RecordSession(duration); ActualizarSessionHistoryUI(); }

                // Check for crashes (Professional Insight)
                var analysis = _crashReporter.AnalyzeLastCrash(sessionStart);
                if (analysis != null)
                {
                    AgregarLog("💥 Crash detectado. Mostrando diagnóstico Nebula...");
                    SwitchToModule(new CrashAnalysisView(analysis, GameFolder));
                    
                    // Auto-report to Discord if configured
                    if (!string.IsNullOrEmpty(_session.CrashWebhookUrl))
                    {
                        string summary = _crashReporter.CheckForCrash(sessionStart) ?? "Error descriptivo no disponible.";
                        await _crashReporter.ReportToDiscordAsync(summary, _session.Username);
                        AgregarLog("✅ Crash reportado al servidor automáticamente.");
                    }
                }

                _discord.SetIdle();
            }
             catch (Exception ex) { AgregarLog($"✗ Error: {ex.Message}"); Show(); }
            finally { PlayButton.IsEnabled = true; PlayButton.Content = "▶  JUGAR"; }
        }

        // Removed old simple log analyzer, now using CrashReporterService.CrashAnalysis

        private async Task<int> LanzarMinecraft(MinecraftProfile profile)
        {
            // Verificación de Conflictos (Imp 11)
            VerificarConflictosDeMods();

            var mcLauncher = new McGameLauncher(GameFolder, profile.RamGB, _session.Username,
                _session.AuthMode == "premium", profile.Version, 
                profile.LoaderType,
                profile.LoaderVersion, 
                manualJavaPath: profile.JavaPath);
            mcLauncher.OnLog      += msg => AgregarLog(msg);
            mcLauncher.OnProgress += pct => Dispatcher.Invoke(() => MainProgressBar.Value = pct);
            
            // SFX: Inicio de motor (Imp 19)
            try { System.Media.SystemSounds.Exclamation.Play(); } catch { }

            return await mcLauncher.LaunchAsync();
        }

        private void VerificarConflictosDeMods()
        {
            try {
                string modsDir = System.IO.Path.Combine(GameFolder, "mods");
                if (!Directory.Exists(modsDir)) return;

                var files = Directory.GetFiles(modsDir, "*.jar");
                var names = new HashSet<string>();
                foreach(var f in files) {
                    string name = System.IO.Path.GetFileNameWithoutExtension(f).ToLower();
                    if (name.Contains("rubidium") && name.Contains("embeddium")) {
                        AgregarLog("⚠ Conflicto detectado: Rubidium y Embeddium juntos causan crash.");
                        MessageBox.Show("Se detectó un conflicto entre Rubidium y Embeddium.\nEjecutá 'Reparar Pack' para una instalación limpia.", "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            } catch { }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ADMIN — PUBLISH UPDATE
        // ══════════════════════════════════════════════════════════════════
        private async void PublicarActualizacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("🚀 Publicando actualización global...");
            try
            {
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nebula-pub-" + Guid.NewGuid().ToString("N"));
                string zipPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "client-assets.zip");
                Directory.CreateDirectory(tempDir);
                foreach (var target in new[] { "options.txt", "config", "shaderpacks", "resourcepacks", "scripts" })
                {
                    string source = System.IO.Path.Combine(GameFolder, target);
                    if      (File.Exists(source))      File.Copy(source, System.IO.Path.Combine(tempDir, target), true);
                    else if (Directory.Exists(source)) CopyDirectory(source, System.IO.Path.Combine(tempDir, target));
                }
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(tempDir, zipPath);
                await RunCommand("gh", $"release delete client-assets-1.0 --repo leaboga/nebula-modpack --yes");
                int code = await RunCommand("gh", $"release create client-assets-1.0 \"{zipPath}\" --repo leaboga/nebula-modpack --title \"Client Assets\" --notes \"Update\"");
                if (code != 0) { AgregarLog($"⚠ gh create falló con código {code}."); return; }
                if (_manifestActual != null)
                {
                    string tempRepo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nebula-repo-sync");
                    if (Directory.Exists(tempRepo)) RobustDelete(tempRepo);
                    await RunCommand("gh", $"repo clone leaboga/nebula-modpack \"{tempRepo}\"");
                    string manifestPath = System.IO.Path.Combine(tempRepo, "versions", _manifestActual.Version, "manifest.json");
                    if (File.Exists(manifestPath))
                    {
                        dynamic manifest = JsonConvert.DeserializeObject(File.ReadAllText(manifestPath))!;
                        manifest.configHash = DateTime.Now.Ticks.ToString();
                        manifest.forceConfigUpdate = true;
                        File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
                        string savedDir = Directory.GetCurrentDirectory();
                        Directory.SetCurrentDirectory(tempRepo);
                        await RunCommand("git", "add ."); await RunCommand("git", "commit -m \"Update Configs Hash\""); await RunCommand("git", "push origin main");
                        Directory.SetCurrentDirectory(savedDir);
                    }
                }
                AgregarLog("✅ Actualización publicada con éxito.");
            }
            catch (Exception ex) { AgregarLog($"❌ Error: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════
        private void ImportarConfigsDeMinecraftOriginal()
        {
            string originalMc = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            if (!Directory.Exists(originalMc)) return;
            foreach (var target in new[] { "options.txt", "config", "shaderpacks", "resourcepacks" })
            {
                try
                {
                    string source = System.IO.Path.Combine(originalMc, target);
                    string dest   = System.IO.Path.Combine(GameFolder,  target);
                    if      (File.Exists(source)      && !File.Exists(dest)) File.Copy(source, dest);
                    else if (Directory.Exists(source) && (!Directory.Exists(dest) || Directory.GetFileSystemEntries(dest).Length == 0)) CopyDirectory(source, dest);
                }
                catch { }
            }
        }

        private void RobustDelete(string path)
        {
            try { var d = new DirectoryInfo(path) { Attributes = FileAttributes.Normal }; foreach (var i in d.GetFileSystemInfos("*", SearchOption.AllDirectories)) i.Attributes = FileAttributes.Normal; d.Delete(true); }
            catch (Exception ex) { AgregarLog($"⚠ RobustDelete: {ex.Message}"); }
        }

        private Task<int> RunCommand(string cmd, string args)
        {
            return Task.Run(() =>
            {
                try
                {
                    var sb  = new StringBuilder();
                    var psi = new ProcessStartInfo(cmd, args) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
                    var proc = Process.Start(psi); if (proc == null) return -1;
                    proc.OutputDataReceived += (s, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
                    proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
                    proc.BeginOutputReadLine(); proc.BeginErrorReadLine();
                    proc.WaitForExit();
                    if (proc.ExitCode != 0 && sb.Length > 0) AgregarLog($"[{cmd}] {sb.ToString().Trim()}");
                    return proc.ExitCode;
                }
                catch (Exception ex) { AgregarLog($"⚠ RunCommand({cmd}): {ex.Message}"); return -1; }
            });
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string f in Directory.GetFiles(source)) try { File.Copy(f, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(f)), true); } catch { }
            foreach (string d in Directory.GetDirectories(source)) CopyDirectory(d, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(d)));
        }
        // ── Performance & HW ────────────────────────────────────────────────
        private void ActualizarMonitores()
        {
            try {
                // Launcher RAM
                long mem = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
                RamPerfText.Text = $"{mem} MB";

                // Simple CPU approximation (scaled by core count dummy for UI feel)
                double cpu = _rnd.Next(1, 4); // Dummy for now without package
                if (_gameProcess != null && !_gameProcess.HasExited) cpu += _rnd.Next(10, 30);
                CpuPerfText.Text = $"{cpu}%";
            } catch { }
        }

        private void AplicarPrioridad()
        {
            if (_gameProcess == null || _gameProcess.HasExited) return;
            try {
                _gameProcess.PriorityClass = ProcessPriorityClass.High;
                AgregarLog("🚀 Prioridad de proceso establecida en ALTA.");
            } catch { }
        }

        // ── UI Helpers ──────────────────────────────────────────────────────
        private void AnimateView(FrameworkElement element)
        {
            element.Visibility = Visibility.Visible;
            var sb = (Storyboard)FindResource("TabChangeEffect");
            sb.Begin(element);
        }

        public void CambiarVista(string vista)
        {
            if (HomeView == null || ModulesContainer == null) return;
            StopCurrentModule();
            
            // Atomic cleanup
            ModulesContainer.Visibility = Visibility.Collapsed;
            HomeView.Visibility = Visibility.Collapsed;
            ModulesContainer.Content = null; 

            switch (vista)
            {
                case "home":
                    this.Title = "Nebula Launcher — Inicio";
                    HomeView.Visibility = Visibility.Visible;
                    HomeView.Opacity = 1;
                    AnimateView(HomeView);
                    ActualizarGreeting();
                    ActualizarVersionesEnHome();
                    break;
                case "changelog":
                    this.Title = "Nebula Launcher — Novedades";
                    SwitchToModule(new ChangelogView());
                    break;
                case "settings":
                    this.Title = "Nebula Launcher — Configuración";
                    SwitchToModule(new ConfigView(this));
                    break;
                case "social":
                    this.Title = "Nebula Launcher — Comunidad";
                    SwitchToModule(new SocialView(_session.ServerIp, _session.Username));
                    break;
                case "perf":
                    this.Title = "Nebula Launcher — Rendimiento";
                    SwitchToModule(new PerformanceView(this));
                    break;
                case "screenshots":
                    this.Title = "Nebula Launcher — Capturas";
                    SwitchToModule(new ScreenshotsView(GameFolder));
                    break;
                case "modmanager":
                    this.Title = "Nebula — Administrar";
                    var mv = new ModManagerView(GameFolder, CurrentProfile);
                    mv.OnSyncRequested += SincronizarTodoAsync;
                    SwitchToModule(mv);
                    break;
                case "modhub":
                    this.Title = "Nebula — Mod Hub";
                    SwitchToModule(new VaultView(GameFolder, CurrentProfile));
                    break;
                case "crash":
                    this.Title = "Nebula Launcher — Diagnóstico";
                    SwitchToModule(new CrashDiagnosticView(_crashReporter));
                    break;
                case "map":
                    this.Title = "Nebula Launcher — Mapa";
                    SwitchToModule(new BlueMapView(_session.ServerIp, _session.BlueMapPort, _session.BlueMapId));
                    break;
            }
        }

        private void ActualizarVersionesEnHome()
        {
            if (CurrentProfile == null || VersionComboBox == null) return;
            // Removed the hijacking of VersionComboBox with Vanilla versions.
            // VersionComboBox is meant for the Modpack server versions via CargarVersionesAsync.
            _ = CargarVersionesAsync();
        }

        private void LimpiarCache_Click(object sender, RoutedEventArgs e)
        {
            try {
                string[] folders = { "logs", "crash-reports", "screenshots", "web-cache" };
                foreach(var f in folders) {
                    string path = System.IO.Path.Combine(GameFolder, f);
                    if (Directory.Exists(path)) Directory.Delete(path, true);
                }
                AgregarLog("🧹 Caché y archivos temporales eliminados con éxito.");
                MessageBox.Show("Caché y archivos temporales limpiados.", "Limpieza completada", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) { AgregarLog($"❌ Error limpiando caché: {ex.Message}"); }
        }

        private bool _isMinimal = false;
        private double _originalWidth;
        private void ToggleMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}
