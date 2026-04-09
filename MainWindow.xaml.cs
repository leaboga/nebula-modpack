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
using NebulaLauncher.Services;
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
        public string GameFolder => PathService.GetInstanceFolder(CurrentProfile?.Id ?? "default");

        // ── Theme brushes ─────────────────────────────────────────────────
        private static readonly SolidColorBrush BrushOnline  = new(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly SolidColorBrush BrushOffline = new(Color.FromRgb(0xEF, 0x44, 0x44));


        private const string UpdateCheckUrl = "https://api.github.com/repos/leaboga/nebula-modpack/releases/latest";
        
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

            PathService.Initialize();
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

                // SINGLE SOURCE OF TRUTH: Set real version in footer
                VersionFooterLabel.Text = $"{KrakenStrings.LauncherName} v{VersionManager.GetCurrentVersion()}";
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
                
                string liveVersion = VersionManager.GetCurrentVersion();
                
                Dispatcher.Invoke(() => {
                    VersionFooterLabel.Text = $"KRAKEN ENGINE v{liveVersion}";
                    AgregarLog($"🛡️ Sistema Operativo Kraken v{liveVersion} — Núcleo estable.");
                    
                    if (_session.AuthMode == "offline" && string.IsNullOrEmpty(_session.Username))
                        NickTextBox.Focus();
                });
                
                // Diferir update check para no quitar prioridad al juego
                await Task.Delay(2000);
                
                // INTEGRITY SELF-TEST
                VersionManager.RunSelfTests(msg => Debug.WriteLine($"[Versioning] {msg}"));

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
                string nebulaUrl = "https://images.unsplash.com/photo-1551244072-5d12893278ab?q=80&w=1000"; // Deep Ocean Night
                if (hour >= 6 && hour < 12)  nebulaUrl = "https://images.unsplash.com/photo-1439066615861-d1af74d74000?q=80&w=1000"; // Ocean Morning
                if (hour >= 12 && hour < 19) nebulaUrl = "https://images.unsplash.com/photo-1505118380757-91f5f45d8de4?q=80&w=1000"; // Deep Blue Evening
                
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
                string localV = VersionManager.GetCurrentVersion();
                using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher");
                
                var response = await http.GetStringAsync(UpdateCheckUrl);
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                if (root == null) return;

                string remoteTag = root.tag_name?.ToString() ?? "";
                string remoteV   = VersionManager.CleanVersion(remoteTag);
                
                AgregarLog($"🔍 Auditoría de Actualización: Local={localV} | Remota={remoteV}");

                // CRITICAL: Semantic comparison prevents loops
                if (!VersionManager.IsNewer(localV, remoteV)) 
                {
                    Dispatcher.Invoke(() => {
                        UpdateBadge.Visibility = Visibility.Collapsed;
                    });
                    return;
                }

                string changelog = root.name?.ToString() ?? "Nueva versión disponible";
                
                _updateDownloadUrl = null;
                if (root.assets != null)
                {
                    foreach (var asset in root.assets)
                    {
                        string assetName = asset.name?.ToString() ?? "";
                        // TARGET: Explicit binary match (ignoring case)
                        if (assetName.Equals("NebulaLauncher.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            _updateDownloadUrl = asset.browser_download_url?.ToString();
                            break;
                        }
                    }
                    
                    // Fallback: If specific name not found, take the first EXE
                    if (string.IsNullOrEmpty(_updateDownloadUrl))
                    {
                        foreach (var asset in root.assets)
                        {
                            if (asset.name?.ToString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _updateDownloadUrl = asset.browser_download_url?.ToString();
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(_updateDownloadUrl))
                {
                    AgregarLog("⚠ No se encontró un binario (.exe) válido en la release remota. Abortando update.");
                    return;
                }

                _updateVersion = remoteV;

                Dispatcher.Invoke(() =>
                {
                    UpdateBadge.Text       = "⚡ NUEVA CORE v" + remoteV;
                    UpdateBadge.Visibility = Visibility.Visible;
                    UpdateBadge.ToolTip    = $"Detectada v{remoteV}: " + changelog;
                    AgregarLog($"✨ [Actualización] Kraken v{remoteV} detectado. Cliqueá en el badge para instalar.");
                });
            }
            catch (Exception ex) { AgregarLog("⚠ Error en auditoría de versión: " + ex.Message); }
        }

        private async void UpdateBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_updateVersion)) return;

            var result = MessageBox.Show(
                "Nueva version disponible: v" + _updateVersion + "\n\n" +
                UpdateBadge.ToolTip + "\n\n" +
                "El launcher se descargará y reiniciará. ¿Continuar?",
                "Actualización",
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
                string currentExe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;

                AgregarLog("Descargando actualizacion...");

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher");
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                
                // Use a clean temp folder to avoid access conflicts
                string updateDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NebulaUpdate_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(updateDir);
                string tempExe = System.IO.Path.Combine(updateDir, "NebulaLauncher.exe");
                await File.WriteAllBytesAsync(tempExe, bytes);

                int pid = Process.GetCurrentProcess().Id;
                string batContent = "@echo off\n" +
                                   "title Nebula Core Updater\n" +
                                   "echo [UPDATE] Aguardando el cierre de procesos activos...\n" +
                                   $"taskkill /F /PID {pid} > nul 2>&1\n" +
                                   "timeout /t 3 /nobreak > nul\n" +
                                   "set /a count=0\n" +
                                   ":loop\n" +
                                   "set /a count+=1\n" +
                                   "echo [UPDATE] Intento de reemplazo %count% de 10...\n" +
                                   "copy /Y \"" + tempExe + "\" \"" + currentExe + "\"\n" +
                                   "if errorlevel 1 (\n" +
                                   "    if %count% geq 10 goto failed\n" +
                                   "    timeout /t 2 /nobreak > nul\n" +
                                   "    goto loop\n" +
                                   ")\n" +
                                   "echo [UPDATE] Motor actualizado con éxito. Reiniciando...\n" +
                                   "start \"\" \"" + currentExe + "\"\n" +
                                   "rmdir /s /q \"" + updateDir + "\"\n" +
                                   "del \"%~f0\"\n" +
                                   "exit\n" +
                                   ":failed\n" +
                                   "echo [ERROR] No se pudo sobrescribir el motor galáctico. El archivo sigue bloqueado.\n" +
                                   "pause\n" +
                                   "exit\n";

                string updaterBat = System.IO.Path.Combine(updateDir, "nebula_updater.bat");
                await File.WriteAllTextAsync(updaterBat, batContent);

                AgregarLog("🔄 Reiniciando para aplicar la actualización...");

                Process.Start(new ProcessStartInfo("cmd.exe", "/C \"" + updaterBat + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                _cerrarDeVerdad = true;
                Environment.Exit(0);
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
            
            // Run self-tests on startup
            Task.Run(() => VersionManager.RunSelfTests(_ => { }));
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
        private void Nav_Hosting_Checked(object sender, RoutedEventArgs e)     { CambiarVista("hosting"); }
        private void Nav_LocalHost_Checked(object sender, RoutedEventArgs e)    { CambiarVista("localhost"); }
        private void Nav_Modpacks_Checked(object sender, RoutedEventArgs e)     { CambiarVista("modpacks"); }
        
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
            btn.Content   = "⏳ Creando backup...";
            try
            {
                string path = await _backupService.CreateBackupAsync(msg => AgregarLog(msg));
                MessageBox.Show($"Backup creado exitosamente:\n{System.IO.Path.GetFileName(path)}",
                                "Backup completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"❌ Error en backup: {ex.Message}"); }
            finally { btn.IsEnabled = true; btn.Content = "💾 Crear Backup Ahora"; }
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
            LoggerService.Log(mensaje);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string time = $"[{DateTime.Now:HH:mm:ss}] ";
                    var runTime = new Run(time) { Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x42, 0x66)) };
                    var runText = new Run(mensaje);

                    // Syntax Highlighting simple
                    if (mensaje.StartsWith("✅") || mensaje.StartsWith("✓")) runText.Foreground = Brushes.LightGreen;
                    else if (mensaje.StartsWith("❌") || mensaje.StartsWith("✗") || mensaje.Contains("Error")) runText.Foreground = Brushes.Salmon;
                    else if (mensaje.StartsWith("⚠️") || mensaje.Contains("Warning")) runText.Foreground = Brushes.Gold;
                    else if (mensaje.StartsWith("🚀") || mensaje.StartsWith("⚡")) runText.Foreground = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
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
            
            Task.Run(() => { try { File.AppendAllText(PathService.LogFile, $"[{DateTime.Now:HH:mm:ss}] {mensaje}\n"); } catch { } });
        }

        // ══════════════════════════════════════════════════════════════════
        //  SESSION PERSISTENCE
        // ══════════════════════════════════════════════════════════════════
        private void CargarSesion()
        {
            _isInitializing = true;
            try
            {
                if (File.Exists(PathService.SessionFile))
                    _session = JsonConvert.DeserializeObject<UserSession>(File.ReadAllText(PathService.SessionFile)) ?? new UserSession();
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
            _isInitializing = false;
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
                        File.WriteAllText(PathService.SessionFile, json);
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
            string greeting = hour < 12 ? "Buenos días" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            
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
                ? $"{greeting}, {_session.Username} 👋\n📢 {currentNews}"
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
            if (!File.Exists(PathService.LogFile)) { AgregarLog("ℹ️ No hay log guardado aún."); return; }
            try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.LogFile}\"", UseShellExecute = true }); }
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
                MessageBox.Show("Sincronización y reparación completada con éxito.", "KRAKEN Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
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
                { AgregarLog("⚠️ No se pudieron cargar las versiones."); return; }

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
                    // Version label removed from UI
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
                else AgregarLog("⚠️ La autenticación no devolvió sesión válida.");
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
                AgregarLog($"📂 Instancia sincronizada con el perfil activo.");
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
            { AgregarLog("⚠️ Ingresa un nombre de usuario."); MessageBox.Show("Ingresa un nombre primero.", "Sin usuario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (_session.Username.Length < 3) { AgregarLog("⚠️ El nombre debe tener al menos 3 caracteres."); return; }
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

                // Cloud Sync
                if (!string.IsNullOrEmpty(_session.CloudPath))
                {
                    AgregarLog("☁️ Iniciando respaldo en la nube...");
                    string zip = await _backupService.CreateBackupAsync();
                    await _backupService.CopyToCloudAsync(zip, _session.CloudPath, msg => AgregarLog(msg));
                }

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
                manualJavaPath: profile.JavaPath,
                customSplash: _session.CustomSplashText,
                isOverlay: _session.IsOverlayEnabled);
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
        private async void PublicarLauncher_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("🚀 Iniciando publicación de MOTOR CORE...");
            try
            {
                // 1. Rebuild en modo Release
                AgregarLog("🔨 Compilando binario final (Release)...");
                int buildResult = await RunCommand("dotnet", "publish NebulaLauncher.csproj -c Release -r win-x64 --self-contained true");
                if (buildResult != 0) { AgregarLog("❌ Error: Falló la compilación del motor."); return; }

                // 2. Extraer versión REAL del binario generado
                string publishPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "bin", "Release", "net8.0-windows", "win-x64", "publish", "NebulaLauncher.exe");
                
                // Fallback attempt to find the publish folder
                if (!File.Exists(publishPath))
                    publishPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release", "net8.0-windows", "win-x64", "publish", "NebulaLauncher.exe");

                if (!File.Exists(publishPath))
                {
                    AgregarLog($"❌ Error: No se encontró el binario en '{publishPath}'");
                    return;
                }

                var info = FileVersionInfo.GetVersionInfo(publishPath);
                string realV = VersionManager.CleanVersion(info.ProductVersion ?? info.FileVersion ?? "1.0.0");
                
                // PRE-FLIGHT INTEGRITY CHECK: Prevents uploading stale binaries
                string currentV = VersionManager.GetCurrentVersion();
                if (realV != currentV)
                {
                    AgregarLog($"❌ ABORTANDO: Se detectó una inconsistencia crítica.");
                    AgregarLog($"Binario Destino: v{realV}");
                    AgregarLog($"Entorno Local:   v{currentV}");
                    AgregarLog("Asegúrate de haber guardado cambios en el .csproj y recompilado.");
                    return;
                }

                AgregarLog($"🛡️ Integridad verificada: Motor v{realV} listo para el Abismo.");

                // 3. Crear Release en GitHub
                string tag = $"v{realV}";
                AgregarLog($"☁ Subiendo release '{tag}' a GitHub...");
                
                // Borrar release vieja si existe (opcional, pero ayuda a corregir errores de dedo)
                await RunCommand("gh", $"release delete {tag} -y --repo leaboga/nebula-modpack");
                
                int code = await RunCommand("gh", $"release create {tag} \"{publishPath}\" --repo leaboga/nebula-modpack --title \"KRAKEN Launcher v{realV}\" --notes \"Actualización obligatoria del motor core.\"");
                
                if (code == 0)
                {
                    AgregarLog($"✅ Publicación de MOTOR v{realV} completada.");
                    MessageBox.Show($"El Motor Core v{realV} ha sido desplegado.", "Kraken Update", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else AgregarLog($"⚠ Fallo al subir a GitHub (Código {code}).");
            }
            catch (Exception ex) { AgregarLog($"❌ Error fatal en publicación de motor: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        private async void PublicarActualizacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("🚀 Iniciando publicación de ASSETS (Mods/Configs)...");
            try
            {
                // 1. Determinar Nueva Versión (SemVer Patch default)
                string currentV = _manifestActual?.Version ?? "1.0.0";
                string nextV    = VersionManager.Increment(currentV, VersionSegment.Patch);
                AgregarLog($"📦 Versionado de Assets: {currentV} ➔ {nextV}");

                // 2. Empaquetar Assets Locales
                string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kraken-pub-" + Guid.NewGuid().ToString("N"));
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

                // 3. Crear Release en GitHub con Tag Dinámico
                string tag = $"v{nextV}-assets";
                AgregarLog($"☁ Subiendo release '{tag}' a GitHub...");
                int code = await RunCommand("gh", $"release create {tag} \"{zipPath}\" --repo leaboga/nebula-modpack --title \"Assets v{nextV}\" --notes \"Actualización automática de configuración y mods.\"");
                if (code != 0) { AgregarLog($"⚠ Fallo al crear release (código {code}). Verificá credenciales de gh."); return; }

                // 4. Sincronizar Repositorio de Manifiesto
                string tempRepo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kraken-repo-sync");
                if (Directory.Exists(tempRepo)) RobustDelete(tempRepo);
                await RunCommand("gh", $"repo clone leaboga/nebula-modpack \"{tempRepo}\"");

                // Crear nueva carpeta de versión
                string newVersionDir = System.IO.Path.Combine(tempRepo, "versions", nextV);
                Directory.CreateDirectory(newVersionDir);
                string manifestPath = System.IO.Path.Combine(newVersionDir, "manifest.json");

                // Generar nuevo manifiesto basado en el actual
                if (_manifestActual != null)
                {
                    _manifestActual.Version = nextV;
                    _manifestActual.ConfigHash = DateTime.Now.Ticks.ToString();
                    _manifestActual.ForceConfigUpdate = true;
                    File.WriteAllText(manifestPath, JsonConvert.SerializeObject(_manifestActual, Formatting.Indented));
                    
                    // Actualizar versions-index.json
                    string indexPath = System.IO.Path.Combine(tempRepo, "versions-index.json");
                    if (File.Exists(indexPath))
                    {
                        var index = JsonConvert.DeserializeObject<VersionsIndex>(File.ReadAllText(indexPath));
                        if (index != null)
                        {
                            index.LatestVersion = nextV;
                            index.AvailableVersions.Insert(0, new VersionEntry { 
                                Version = nextV, 
                                Label = $"v{nextV} ({DateTime.Now:dd/MM HH:mm})",
                                ManifestUrl = $"https://raw.githubusercontent.com/leaboga/nebula-modpack/main/versions/{nextV}/manifest.json"
                            });
                            File.WriteAllText(indexPath, JsonConvert.SerializeObject(index, Formatting.Indented));
                        }
                    }

                    // Push a GitHub
                    string savedDir = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(tempRepo);
                    await RunCommand("git", "add ."); 
                    await RunCommand("git", "commit -m \"Release v" + nextV + "\""); 
                    await RunCommand("git", "push origin main");
                    Directory.SetCurrentDirectory(savedDir);
                }

                AgregarLog($"✅ Publicación v{nextV} completada satisfactoriamente.");
                MessageBox.Show($"La versión {nextV} ha sido desplegada en el enjambre.", "Éxito Galáctico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"❌ Error fatal en publicación: {ex.Message}"); }
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
            try 
            {
                if (!Directory.Exists(path)) return;
                var d = new DirectoryInfo(path) { Attributes = FileAttributes.Normal }; 
                foreach (var i in d.GetFileSystemInfos("*", SearchOption.AllDirectories)) i.Attributes = FileAttributes.Normal; 
                d.Delete(true); 
            }
            catch (Exception ex) 
            { 
                AgregarLog($"⚠ RobustDelete (Fase 1): {ex.Message}. Intentando desintegración forzada..."); 
                try
                {
                    // Fallback agresivo para locks de Windows (Memoria de Usuario)
                    var psi = new ProcessStartInfo("cmd.exe", $"/c rd /s /q \"{path}\"") { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(psi)?.WaitForExit();
                    if (Directory.Exists(path)) AgregarLog("❌ Error: La carpeta resiste la eliminación forzada.");
                }
                catch (Exception ex2) { AgregarLog($"⚠ RobustDelete (Fase 2): {ex2.Message}"); }
            }
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
                    CurrentViewLabel.Text = "CENTRO DE OPERACIONES";
                    ViewTitleLabel.Text = "Bienvenido, Comandante";
                    HomeView.Visibility = Visibility.Visible;
                    HomeView.Opacity = 1;
                    AnimateView(HomeView);
                    ActualizarGreeting();
                    ActualizarVersionesEnHome();
                    break;
                case "changelog":
                    CurrentViewLabel.Text = "NOTIFICACIONES";
                    ViewTitleLabel.Text = "Bitácora de Versiones";
                    SwitchToModule(new ChangelogView());
                    break;
                case "settings":
                    CurrentViewLabel.Text = "SISTEMAS";
                    ViewTitleLabel.Text = "Configuración del Iniciador";
                    SwitchToModule(new ConfigView(this));
                    break;
                case "social":
                    CurrentViewLabel.Text = "RED EXTERNA";
                    ViewTitleLabel.Text = "Comunidad KRAKEN";
                    SwitchToModule(new SocialView(_session.ServerIp, _session.Username));
                    break;
                case "perf":
                    CurrentViewLabel.Text = "OPTIMIZACIÓN";
                    ViewTitleLabel.Text = "Rendimiento y Memoria";
                    SwitchToModule(new PerformanceView(this));
                    break;
                case "screenshots":
                    CurrentViewLabel.Text = "ARCHIVOS";
                    ViewTitleLabel.Text = "Capturas de Despliegue";
                    SwitchToModule(new ScreenshotsView(GameFolder));
                    break;
                case "modmanager":
                    CurrentViewLabel.Text = "LOGÍSTICA";
                    ViewTitleLabel.Text = "Gestión de Módulos (Local)";
                    var mv = new ModManagerView(GameFolder, CurrentProfile);
                    mv.OnSyncRequested += SincronizarTodoAsync;
                    SwitchToModule(mv);
                    break;
                case "modhub":
                    CurrentViewLabel.Text = "CENTRO DE RECURSOS";
                    ViewTitleLabel.Text = "Biblioteca de Mods";
                    SwitchToModule(new VaultView(GameFolder, CurrentProfile));
                    break;
                case "crash":
                    CurrentViewLabel.Text = "DIAGNÓSTICO";
                    ViewTitleLabel.Text = "Reportes de Error";
                    SwitchToModule(new CrashDiagnosticView(_crashReporter));
                    break;
                case "map":
                    CurrentViewLabel.Text = "INTELIGENCIA";
                    ViewTitleLabel.Text = "Servicio de Cartografía";
                    SwitchToModule(new BlueMapView(_session.ServerIp, _session.BlueMapPort, _session.BlueMapId));
                    break;
                case "hosting":
                    CurrentViewLabel.Text = "INFRAESTRUCTURA";
                    ViewTitleLabel.Text = "Servicios Hosting (BETA)";
                    SwitchToModule(new HostingServiceView());
                    break;
                case "localhost":
                    CurrentViewLabel.Text = "NODOS LOCALES";
                    ViewTitleLabel.Text = "Servidor de Pruebas";
                    SwitchToModule(new ServerHostView());
                    break;
                case "modpacks":
                    CurrentViewLabel.Text = "MODPACK HUB";
                    ViewTitleLabel.Text = "Catálogo de Expediciones";
                    SwitchToModule(new ModpackView());
                    break;
            }
            
            if (ActiveProfileLabel != null && CurrentProfile != null)
            {
                ActiveProfileLabel.Text = CurrentProfile.Name;
                ActiveVersionLabel.Text = CurrentProfile.Version ?? "---";
                ActiveLoaderLabel.Text = (CurrentProfile.LoaderType ?? "Vanilla").ToUpperInvariant();
                
                if (ProfilePathLabel != null)
                {
                    string pPath = System.IO.Path.Combine(PathService.InstancesFolder, CurrentProfile.Id);
                    ProfilePathLabel.Text = pPath;
                    ProfilePathLabel.ToolTip = pPath;
                }
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
        public void RecargarPerfiles()
        {
            // Logic to refresh the home view profiles list
            // If the Home view is active, we might need to recreate it or call its refresh method
            CambiarVista("home");
        }
    }
}
