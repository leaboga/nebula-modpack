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

namespace NebulaLauncher
{

    public partial class MainWindow : Window
    {
        // â”€â”€ Paths â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public MinecraftProfile? CurrentProfile => _session.Profiles.Find(p => p.Id == _session.CurrentProfileId) ?? (_session.Profiles.Count > 0 ? _session.Profiles[0] : null);
        public string GameFolder => PathService.GetInstanceFolder(CurrentProfile?.Id ?? "default");

        // â”€â”€ Theme brushes â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static readonly SolidColorBrush BrushOnline  = new(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly SolidColorBrush BrushOffline = new(Color.FromRgb(0xEF, 0x44, 0x44));


        private const string UpdateCheckUrl = "https://api.github.com/repos/leaboga/nebula-modpack/releases/latest";
        
        // â”€â”€ Services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private readonly SocialService          _socialService    = new();
        private readonly ServerStatusCache      _cache            = new();
        private readonly ChangelogService       _changelogService = new();
        private readonly SkinService            _skinService      = new();
        private readonly SessionHistoryService  _historyService   = new();
        private readonly DiscordRPCService      _discord          = new();
        private BackupService                   _backupService    = null!;
        private CrashReporterService            _crashReporter    = null!;
        private ModSyncer                       _syncer           = null!;

        // â”€â”€ State â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  WINDOW CONTROLS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
        public CrashReporterService GetCrashReporter() => _crashReporter;
        private UserSession   _session         = new();
        private ModManifest?  _manifestActual;
        private VersionsIndex? _versionsIndex;
        private bool          _cerrarDeVerdad  = false;
        private bool          _isInitializing  = false;
        private DispatcherTimer _updateTimer   = null!;
        private readonly System.Windows.Media.MediaPlayer _bgPlayer = new();
        private bool _isMusicPlaying = false;

        // â”€â”€ Particles â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private readonly List<(Ellipse dot, double vx, double vy)> _particles = new();
        private DispatcherTimer? _particleTimer;
        private readonly Random _rnd = new();

        // â”€â”€ Notifications (friend tracking) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
            InitializeModernServices();

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
                    AgregarLog($"ðŸ›¡ï¸ Sistema Operativo Kraken v{liveVersion} â€” NÃºcleo estable.");
                    
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

        private void InitializeModernServices()
        {
            NavigationService.Instance.Initialize(ModulesContainer, HomeView, CurrentViewLabel, ViewTitleLabel, () => StopCurrentModule());
            EffectService.Instance.Initialize(ParticleCanvas, LauncherBackground);
            NotificationService.Instance.Initialize(NotificationArea);
            
            EffectService.Instance.StartParticles();
            EffectService.Instance.UpdateBackground(_session);
            EffectService.Instance.ApplyThemeColor(_session, AvatarInitial, PercentageLabel);
        }// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PARTICLES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void IniciarParticulas() => EffectService.Instance.StartParticles();

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  SERVER STATUS + FRIEND NOTIFICATIONS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
                        PlayersText.Text = $"Ãšltimo: {cached.Status.OnlinePlayers} jug.";
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
                    AgregarLog($"ðŸ‘‹ {p} se ha unido al servidor.");
                    // Mejora: Toast feedback visual rÃ¡pido
                    Dispatcher.Invoke(() => {
                        StatusText.Text = $"âœ¨ {p} entrÃ³!";
                        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                        timer.Tick += (s, e) => { StatusText.Text = "Online"; timer.Stop(); };
                        timer.Start();
                    });
                }
            }
            _lastOnlinePlayers = currentPlayers;
        }

        public void ActualizarFondo() => EffectService.Instance.UpdateBackground(_session);

        public void ActualizarColorTema() => EffectService.Instance.ApplyThemeColor(_session, AvatarInitial, PercentageLabel);

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  LAUNCHER UPDATE CHECK
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private string? _updateDownloadUrl;
        private string? _updateVersion;
        private string? _autoUpdateAttemptedVersion;
        private bool _isAutoUpdating;

        private async Task CheckForLauncherUpdate()
        {
            try
            {
                string localV = VersionManager.GetCurrentVersion();
                string currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher");
                
                var response = await http.GetStringAsync(UpdateCheckUrl);
                var root = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response);
                if (root == null) return;

                string remoteTag = root.tag_name?.ToString() ?? "";
                string remoteV   = VersionManager.CleanVersion(remoteTag);
                
                AgregarLog($"ðŸ” AuditorÃ­a de ActualizaciÃ³n: Local={localV} | Remota={remoteV}");

                // CRITICAL: Semantic comparison prevents loops
                if (!VersionManager.IsNewer(localV, remoteV)) 
                {
                    _updateDownloadUrl = null;
                    _updateVersion = null;
                    UpdateDiagnosticsService.MarkNoUpdate(localV, remoteV);
                    Dispatcher.Invoke(() => {
                        UpdateBadge.Visibility = Visibility.Collapsed;
                    });
                    return;
                }

                string changelog = root.name?.ToString() ?? "Nueva versiÃ³n disponible";
                
                _updateDownloadUrl = null;
                string selectedAssetName = string.Empty;
                string currentExeName = System.IO.Path.GetFileName(Environment.ProcessPath ?? "KrakenLauncher.exe");
                string[] preferredAssetNames = new[]
                {
                    currentExeName,
                    "KrakenLauncher.exe",
                    "NebulaLauncher.exe"
                };
                if (root.assets != null)
                {
                    foreach (string preferredAssetName in preferredAssetNames)
                    {
                        foreach (var asset in root.assets)
                        {
                            string assetName = asset.name?.ToString() ?? "";
                            if (assetName.Equals(preferredAssetName, StringComparison.OrdinalIgnoreCase))
                            {
                                _updateDownloadUrl = asset.browser_download_url?.ToString();
                                selectedAssetName = assetName;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(_updateDownloadUrl))
                            break;
                    }
                    
                    // Fallback: If specific name not found, take the first EXE
                    if (string.IsNullOrEmpty(_updateDownloadUrl))
                    {
                        foreach (var asset in root.assets)
                        {
                            if (asset.name?.ToString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                _updateDownloadUrl = asset.browser_download_url?.ToString();
                                selectedAssetName = asset.name?.ToString() ?? string.Empty;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(_updateDownloadUrl))
                {
                    UpdateDiagnosticsService.MarkFailure("No se encontro un asset .exe valido en la release remota.");
                    AgregarLog("âš  No se encontrÃ³ un binario (.exe) vÃ¡lido en la release remota. Abortando update.");
                    return;
                }

                _updateVersion = remoteV;
                UpdateDiagnosticsService.MarkCheck(localV, remoteV, selectedAssetName, _updateDownloadUrl, currentExePath);

                Dispatcher.Invoke(() =>
                {
                    UpdateBadge.Text       = "âš¡ NUEVA CORE v" + remoteV;
                    UpdateBadge.Visibility = Visibility.Visible;
                    UpdateBadge.ToolTip    = $"Detectada v{remoteV}: " + changelog;
                    UpdateBadge.IsEnabled  = false;
                    AgregarLog($"âœ¨ [ActualizaciÃ³n] Kraken v{remoteV} detectado. Se inicia la auto-actualizaciÃ³n.");
                });

                if (!_isAutoUpdating && _autoUpdateAttemptedVersion != remoteV)
                {
                    _autoUpdateAttemptedVersion = remoteV;
                    _isAutoUpdating = true;
                    await AplicarUpdateAsync(_updateDownloadUrl, true);
                }
            }
            catch (Exception ex) { AgregarLog("âš  Error en auditorÃ­a de versiÃ³n: " + ex.Message); }
        }

        private async void UpdateBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_updateVersion)) return;

            var result = MessageBox.Show(
                "Nueva version disponible: v" + _updateVersion + "\n\n" +
                UpdateBadge.ToolTip + "\n\n" +
                "El launcher se descargarÃ¡ y reiniciarÃ¡. Â¿Continuar?",
                "ActualizaciÃ³n",
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
                _isAutoUpdating = true;
                await AplicarUpdateAsync(_updateDownloadUrl, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateBadge.Text      = "v" + _updateVersion + " disponible";
                UpdateBadge.IsEnabled = true;
                _isAutoUpdating = false;
            }
        }

        private async Task AplicarUpdateAsync(string downloadUrl, bool isAutomatic)
        {
            try
            {
                string currentExe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(currentExe)) return;
                string currentExeDirectory = System.IO.Path.GetDirectoryName(currentExe) ?? AppDomain.CurrentDomain.BaseDirectory;
                string downloadedAssetName = GetUpdateAssetName(downloadUrl, currentExe);
                string targetExe = System.IO.Path.Combine(currentExeDirectory, downloadedAssetName);

                AgregarLog($"Descargando actualizacion hacia {downloadedAssetName}...");

                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher");
                var bytes = await http.GetByteArrayAsync(downloadUrl);
                
                // Use a clean temp folder to avoid access conflicts
                string updateDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KrakenUpdate_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(updateDir);
                string tempExe = System.IO.Path.Combine(updateDir, downloadedAssetName);
                await File.WriteAllBytesAsync(tempExe, bytes);
                File.WriteAllText(PathService.UpdaterLogFile, string.Empty);
                UpdateDiagnosticsService.MarkApplying(targetExe, isAutomatic);

                int pid = Process.GetCurrentProcess().Id;
                string batContent = "@echo off\n" +
                                   "title Kraken Core Updater\n" +
                                   "echo [UPDATE] Aguardando el cierre de procesos activos...\n" +
                                   $"taskkill /F /PID {pid} > nul 2>&1\n" +
                                   "timeout /t 3 /nobreak > nul\n" +
                                   "set /a count=0\n" +
                                   ":loop\n" +
                                   "set /a count+=1\n" +
                                   "echo [UPDATE] Intento de reemplazo %count% de 10...\n" +
                                   "echo [%date% %time%] copy " + tempExe + " -> " + targetExe + ">> \"" + PathService.UpdaterLogFile + "\"\n" +
                                   "copy /Y \"" + tempExe + "\" \"" + targetExe + "\"\n" +
                                   "if errorlevel 1 (\n" +
                                   "    if %count% geq 10 goto failed\n" +
                                   "    timeout /t 2 /nobreak > nul\n" +
                                   "    goto loop\n" +
                                   ")\n" +
                                   "echo [UPDATE] Motor actualizado con Ã©xito. Reiniciando...\n" +
                                   "if /I not \"" + currentExe + "\"==\"" + targetExe + "\" del /F /Q \"" + currentExe + "\" > nul 2>&1\n" +
                                   "start \"\" \"" + targetExe + "\"\n" +
                                   "rmdir /s /q \"" + updateDir + "\"\n" +
                                   "del \"%~f0\"\n" +
                                   "exit\n" +
                                   ":failed\n" +
                                   "echo [ERROR] No se pudo sobrescribir el motor galÃ¡ctico. El archivo sigue bloqueado.\n" +
                                   "pause\n" +
                                   "exit\n";

                string updaterBat = System.IO.Path.Combine(updateDir, "kraken_updater.bat");
                await File.WriteAllTextAsync(updaterBat, batContent);

                AgregarLog("ðŸ”„ Reiniciando para aplicar la actualizaciÃ³n...");

                Process.Start(new ProcessStartInfo("cmd.exe", "/C \"" + updaterBat + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                UpdateDiagnosticsService.MarkRestartScheduled();
                _cerrarDeVerdad = true;
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                _isAutoUpdating = false;
                AgregarLog("âš  Error al aplicar actualizaciÃ³n: " + ex.Message);
                if (!isAutomatic)
                    MessageBox.Show("Error al aplicar actualizaciÃ³n: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateBadge.IsEnabled = true;
            }
        }

        // Checker periÃ³dico (cada 1 hora)
        private void IniciarUpdateTimer()
        {
            var t = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
            t.Tick += (_, _) => _ = CheckForLauncherUpdate();
            t.Start();
            
            // Run self-tests on startup
            Task.Run(() => VersionManager.RunSelfTests(_ => { }));
        }

        private static string GetUpdateAssetName(string downloadUrl, string currentExe)
        {
            try
            {
                if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
                {
                    string assetName = System.IO.Path.GetFileName(uri.AbsolutePath);
                    if (!string.IsNullOrWhiteSpace(assetName))
                        return assetName;
                }
            }
            catch
            {
            }

            return System.IO.Path.GetFileName(currentExe);
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  SKIN
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  SESSION HISTORY
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  NAVIGATION
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private void Nav_Home_Checked(object sender, RoutedEventArgs e)       { CambiarVista("home"); }
        private void Nav_ConfigSync_Checked(object sender, RoutedEventArgs e) { CambiarVista("configsync"); }
        private void Nav_Changelog_Checked(object sender, RoutedEventArgs e)  { CambiarVista("changelog"); }
        private void Nav_Settings_Checked(object sender, RoutedEventArgs e)   { CambiarVista("settings"); }
        private void Nav_Social_Checked(object sender, RoutedEventArgs e)     { CambiarVista("social"); }
        private void Nav_Perf_Checked(object sender, RoutedEventArgs e)       { CambiarVista("perf"); }
        private void Nav_Screenshots_Checked(object sender, RoutedEventArgs e) { CambiarVista("screenshots"); }
        private void Nav_ModManager_Checked(object sender, RoutedEventArgs e)  { CambiarVista("modmanager"); }
        private void Nav_ModHub_Checked(object sender, RoutedEventArgs e)      { CambiarVista("modhub"); }
        private void Nav_Crash_Checked(object sender, RoutedEventArgs e)       { CambiarVista("crash"); }
        private void Nav_Console_Checked(object sender, RoutedEventArgs e)     { CambiarVista("console"); }
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  COPY IP
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  BACKUP
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async void BackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn       = (Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "â³ Creando backup...";
            try
            {
                string path = await _backupService.CreateBackupAsync(msg => AgregarLog(msg));
                MessageBox.Show($"Backup creado exitosamente:\n{System.IO.Path.GetFileName(path)}",
                                "Backup completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"âŒ Error en backup: {ex.Message}"); }
            finally { btn.IsEnabled = true; btn.Content = "ðŸ’¾ Crear Backup Ahora"; }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  LOG
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
                    if (mensaje.StartsWith("âœ…") || mensaje.StartsWith("âœ“")) runText.Foreground = Brushes.LightGreen;
                    else if (mensaje.StartsWith("âŒ") || mensaje.StartsWith("âœ—") || mensaje.Contains("Error")) runText.Foreground = Brushes.Salmon;
                    else if (mensaje.StartsWith("âš ï¸") || mensaje.Contains("Warning")) runText.Foreground = Brushes.Gold;
                    else if (mensaje.StartsWith("ðŸš€") || mensaje.StartsWith("âš¡")) runText.Foreground = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  SESSION PERSISTENCE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
            string greeting = hour < 12 ? "Buenos dÃ­as" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            
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
                ? $"{greeting}, {_session.Username} ðŸ‘‹\nðŸ“¢ {currentNews}"
                : "Listo para jugar";
        }

        private void MostrarUsuarioPremium(string username)
        {
            if (LoggedUsernameText != null) LoggedUsernameText.Text      = username;
            if (NotLoggedPanel     != null) NotLoggedPanel.Visibility    = Visibility.Collapsed;
            if (LoggedPanel        != null) LoggedPanel.Visibility       = Visibility.Visible;
            ActualizarSidebar();
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  UI EVENTS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
            AgregarLog($"ðŸ“‚ Perfil cambiado a: {_session.Profiles[idx].Name}");
            
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
            
            AgregarLog($"ðŸ—‘ï¸ Perfil '{profileToDelete.Name}' eliminado.");
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
            AgregarLog($"âœ… Perfil '{name}' creado con Ã©xito.");
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
            AgregarLog($"âœ… Perfil '{newName}' clonado con Ã©xito.");
        }

        private void VerLog_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(PathService.LogFile)) { AgregarLog("â„¹ï¸ No hay log guardado aÃºn."); return; }
            try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.LogFile}\"", UseShellExecute = true }); }
            catch (Exception ex) { AgregarLog($"âš ï¸ Error abriendo log: {ex.Message}"); }
        }

        private async void RepararModpack_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) { btn.IsEnabled = false; btn.Content = "â³ Reparando..."; }
            try
            {
                await SincronizarTodoAsync();
                AgregarLog("âœ… SincronizaciÃ³n completada.");
                MessageBox.Show("SincronizaciÃ³n y reparaciÃ³n completada con Ã©xito.", "KRAKEN Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"âŒ Error en reparaciÃ³n: {ex.Message}"); }
            finally { if (btn != null) { btn.IsEnabled = true; btn.Content = "ðŸ› ï¸ Reparar Pack"; } }
        }

        public async Task SincronizarTodoAsync()
        {
            if (CurrentProfile == null) return;
            AgregarLog("ðŸ› ï¸ Iniciando sincronizaciÃ³n total (GitHub)...");
            
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
                    AgregarLog($"âœ“ Perfil '{CurrentProfile.Name}' sincronizado correctamente.");
                }
            }
            else
            {
                AgregarLog("âš  No se pudo obtener el manifiesto de GitHub.");
            }
            
            PlayButton.Content = "â–¶  JUGAR";
        }

        /// <summary>
        /// Verifica si las configs de Pepita cambiaron (via hash remoto).
        /// Si cambiaron y el usuario no es Pepita, muestra un dialogo para que ELIJA si aplicar.
        /// Si se llama con forzar=true (desde admin), aplica sin preguntar.
        /// </summary>
        private async Task AplicarConfigsSiHayCambiosAsync(bool forzar)
        {
            try
            {
                bool esPepita = _session.IsAdmin
                             || _session.Username.Equals("Pepita",  StringComparison.OrdinalIgnoreCase)
                             || _session.Username.Equals("Leandro", StringComparison.OrdinalIgnoreCase);

                string? hashRemoto = await _syncer.ObtenerHashConfigsRemoto();
                if (string.IsNullOrEmpty(hashRemoto))
                {
                    AgregarLog("Info: No se pudo verificar configs de Pepita (sin conexion).");
                    return;
                }

                bool hayNuevasConfigs = hashRemoto != _session.LastAppliedConfigHash;

                if (!hayNuevasConfigs)
                {
                    AgregarLog("Configs al dia (sin cambios de Pepita).");
                    return;
                }

                if (esPepita && !forzar)
                {
                    AgregarLog("Pepita: hay configs nuevas publicadas. Podas aplicarlas desde el panel Config.");
                    return;
                }

                bool aplicar = forzar;
                if (!forzar)
                {
                    var resultado = Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            "Pepita actualizo las configuraciones del modpack!\n\n" +
                            "Deseas aplicar las configs nuevas?\n" +
                            "(Tus opciones personales de controles y graficos seran respetadas)",
                            "Configs de Pepita disponibles",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question));
                    aplicar = resultado == MessageBoxResult.Yes;
                }

                if (aplicar)
                {
                    AgregarLog("Aplicando configs de Pepita...");
                    await _syncer.SincronizarConfigs(sobrescribirTodo: false);
                    _session.LastAppliedConfigHash = hashRemoto;
                    GuardarSesion();
                    Services.NotificationService.Instance.ShowSuccess("Configs de Pepita aplicadas correctamente.");
                }
                else
                {
                    AgregarLog("Configs de Pepita omitidas por eleccion del usuario.");
                }
            }
            catch (Exception ex) { AgregarLog($"Error al verificar configs: {ex.Message}"); }
        }


        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrEmpty(_session.CloudPath))
            {
                try { await CloudService.Instance.SyncToCloud(_session, _session.CloudPath); } catch { }
            }
            GuardarSesion();
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  VERSIONS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async Task CargarVersionesAsync()
        {
            try
            {
                AgregarLog("\uD83D\uDD0D Verificando versiones disponibles...");
                _versionsIndex = await _syncer.ObtenerVersionsIndex();

                if (_versionsIndex?.AvailableVersions == null || _versionsIndex.AvailableVersions.Count == 0)
                { AgregarLog("âš ï¸ No se pudieron cargar las versiones."); return; }

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

                // --- NUEVO: Verificación de Config Oficial ---
                await VerificandoConfigOficialAlCargar();
            }
            catch (Exception ex) { AgregarLog($"\u26A0 Error cargando versiones: {ex.Message}"); }
        }

        private async Task VerificandoConfigOficialAlCargar()
        {
            try
            {
                if (_versionsIndex == null) return;
                
                string? manifestUrl = _versionsIndex.AvailableVersions.Find(v => v.Version == _versionsIndex.LatestVersion)?.ManifestUrl;
                if (string.IsNullOrEmpty(manifestUrl)) return;
                
                var manifest = await _syncer.ObtenerManifest(manifestUrl);
                if (manifest == null) return;

                string versionOficial = manifest.ConfigVersion ?? "1";
                string profileId = CurrentProfile?.Id ?? "default";
                
                string versionAplicada = _session.AppliedConfigVersions.ContainsKey(profileId) 
                    ? _session.AppliedConfigVersions[profileId] : "0";
                
                string versionRechazada = _session.RejectedConfigVersions.ContainsKey(profileId)
                    ? _session.RejectedConfigVersions[profileId] : "0";

                if (versionOficial != versionAplicada && versionOficial != versionRechazada)
                {
                    // Nueva config disponible y no rechazada
                    Dispatcher.Invoke(() => {
                        var res = MessageBox.Show(
                            $"âœ¨ Hay una nueva configuraciÃ³n oficial v{versionOficial} disponible para este perfil.\n\n" +
                            "Incluye optimizaciones de rendimiento, shaders y keybinds recomendados.\n" +
                            "Â¿Deseas aplicarla ahora?\n\n" +
                            "(Tus controles personales serÃ¡n respetados)",
                            "ConfiguraciÃ³n Recomendada",
                            MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (res == MessageBoxResult.Yes)
                        {
                            _ = AplicarConfigOficialAsync(manifest);
                        }
                        else if (res == MessageBoxResult.No)
                        {
                            // Guardar rechazo para no volver a molestar con ESTA versiÃ³n
                            _session.RejectedConfigVersions[profileId] = versionOficial;
                            GuardarSesion();
                            AgregarLog($"ðŸ”” Config oficial v{versionOficial} rechazada por el usuario.");
                        }
                    });
                }
            }
            catch { }
        }

        private async Task AplicarConfigOficialAsync(ModManifest manifest)
        {
            try
            {
                AgregarLog($"ðŸ”„ Aplicando configuraciÃ³n oficial v{manifest.ConfigVersion}...");
                
                // Backup simple
                string backupDir = System.IO.Path.Combine(GameFolder, "backups", "auto-config-v" + manifest.ConfigVersion);
                Directory.CreateDirectory(backupDir);
                foreach (var target in new[] { "options.txt", "config" })
                {
                    string src = System.IO.Path.Combine(GameFolder, target);
                    if (File.Exists(src)) File.Copy(src, System.IO.Path.Combine(backupDir, target), true);
                    else if (Directory.Exists(src)) CopyDirectory(src, System.IO.Path.Combine(backupDir, target));
                }

                await _syncer.SincronizarConfigs(sobrescribirTodo: false);

                string profileId = CurrentProfile?.Id ?? "default";
                _session.AppliedConfigVersions[profileId] = manifest.ConfigVersion;
                _session.RejectedConfigVersions.Remove(profileId);
                GuardarSesion();

                AgregarLog($"âœ… ConfiguraciÃ³n oficial v{manifest.ConfigVersion} aplicada correctamente.");
                NotificationService.Instance.ShowSuccess($"Config oficial v{manifest.ConfigVersion} lista.");
            }
            catch (Exception ex) { AgregarLog($"âš  Error aplicando config oficial: {ex.Message}"); }
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  AUTH
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async void MicrosoftLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false; btn.Content = "Iniciando sesi\u00F3n...";
            AgregarLog("\uD83D\uDD10 Abriendo autenticaci\u00F3n de Microsoft...");
            try
            {
                var session = await AuthService.Instance.LoginMicrosoftAsync();
                if (session != null && !string.IsNullOrEmpty(session.Username))
                {
                    _session.Username = session.Username;
                    _session.AuthMode = "premium";
                    GuardarSesion();
                    MostrarUsuarioPremium(session.Username);
                    AgregarLog($"\u2705 Sesi\u00F3n iniciada como {session.Username}.");
                    await RefrescarSkin();
                }
                else AgregarLog("âš ï¸ La autenticaciÃ³n no devolviÃ³ sesiÃ³n vÃ¡lida.");
            }
            catch (Exception ex)
            {
                AgregarLog($"âŒ Error en login Microsoft: {ex.Message}");
                MessageBox.Show($"Error iniciando sesi\u00F3n:\n{ex.Message}", "Error de autenticaci\u00F3n", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { btn.IsEnabled = true; btn.Content = "Iniciar sesi\u00F3n con Microsoft"; }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            AuthService.Instance.Logout(_session);
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  ADMIN
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
                AgregarLog($"ðŸ“‚ Instancia sincronizada con el perfil activo.");
                _ = CargarVersionesAsync();
            }
            catch (Exception ex) { AgregarLog($"\u274C Error al cambiar instancia: {ex.Message}"); }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  PLAY
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_session.Username))
            { AgregarLog("âš ï¸ Ingresa un nombre de usuario."); MessageBox.Show("Ingresa un nombre primero.", "Sin usuario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (_session.Username.Length < 3) { AgregarLog("âš ï¸ El nombre debe tener al menos 3 caracteres."); return; }
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
                    AgregarLog("ðŸ’¾ Creando backup de seguridad (rÃ¡pido)...");
                    await _backupService.CreateQuickConfigBackupAsync();
                }

                if (turboMode) AgregarLog("âš¡ Modo Turbo activado â€” omitiendo sincronizaciÃ³n de archivos.");

                if (!turboMode && _manifestActual != null)
                {
                    PlayButton.Content = "Sincronizando mods...";
                    _discord.SetActivity("Sincronizando mods...");
                    bool modsOk = await _syncer.SincronizarMods(_manifestActual);
                    if (!modsOk) { AgregarLog("âŒ FallÃ³ la descarga de mods."); return; }

                    // --- CONFIGS DE PEPITA: verificar hash remoto ---
                    PlayButton.Content = "Verificando configs...";
                    await AplicarConfigsSiHayCambiosAsync(forzar: false);
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
                ProgressLabel.Text = exitCode == 0 ? "SesiÃ³n finalizada." : $"Minecraft cerrÃ³ con cÃ³digo {exitCode}.";

                // Record session
                var duration = DateTime.Now - sessionStart;
                if (duration.TotalMinutes >= 1) { _historyService.RecordSession(duration); ActualizarSessionHistoryUI(); }

                // Cloud Sync
                if (!string.IsNullOrEmpty(_session.CloudPath))
                {
                    AgregarLog("â˜ï¸ Iniciando respaldo en la nube...");
                    string zip = await _backupService.CreateBackupAsync();
                    await _backupService.CopyToCloudAsync(zip, _session.CloudPath, msg => AgregarLog(msg));
                }

                // Check for crashes (Professional Insight)
                var analysis = _crashReporter.AnalyzeLastCrash(sessionStart);
                if (analysis != null)
                {
                    AgregarLog("ðŸ’¥ Crash detectado. Mostrando diagnÃ³stico Nebula...");
                    SwitchToModule(new CrashAnalysisView(analysis, GameFolder));
                    
                    // Auto-report to Discord if configured
                    if (!string.IsNullOrEmpty(_session.CrashWebhookUrl))
                    {
                        string summary = _crashReporter.CheckForCrash(sessionStart) ?? "Error descriptivo no disponible.";
                        await _crashReporter.ReportToDiscordAsync(summary, _session.Username);
                        AgregarLog("âœ… Crash reportado al servidor automÃ¡ticamente.");
                    }
                }

                _discord.SetIdle();
            }
             catch (Exception ex) { AgregarLog($"âœ— Error: {ex.Message}"); Show(); }
            finally { PlayButton.IsEnabled = true; PlayButton.Content = "â–¶  JUGAR"; }
        }

        // Removed old simple log analyzer, now using CrashReporterService.CrashAnalysis

        private async Task<int> LanzarMinecraft(MinecraftProfile profile)
        {
            // VerificaciÃ³n de Conflictos (Imp 11)
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
                        AgregarLog("âš  Conflicto detectado: Rubidium y Embeddium juntos causan crash.");
                        MessageBox.Show("Se detectÃ³ un conflicto entre Rubidium y Embeddium.\nEjecutÃ¡ 'Reparar Pack' para una instalaciÃ³n limpia.", "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            } catch { }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  ADMIN â€” PUBLISH UPDATE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        private async void PublicarLauncher_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("ðŸš€ Iniciando publicaciÃ³n de MOTOR CORE...");
            try
            {
                // 1. Rebuild en modo Release
                AgregarLog("ðŸ”¨ Compilando binario final (Release)...");
                int buildResult = await RunCommand("dotnet", "publish NebulaLauncher.csproj -c Release -r win-x64 --self-contained true");
                if (buildResult != 0) { AgregarLog("âŒ Error: FallÃ³ la compilaciÃ³n del motor."); return; }

                // 2. Extraer versiÃ³n REAL del binario generado
                string publishPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "bin", "Release", "net8.0-windows", "win-x64", "publish", "KrakenLauncher.exe");
                
                // Fallback attempt to find the publish folder
                if (!File.Exists(publishPath))
                    publishPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release", "net8.0-windows", "win-x64", "publish", "KrakenLauncher.exe");

                if (!File.Exists(publishPath))
                {
                    AgregarLog($"âŒ Error: No se encontrÃ³ el binario en '{publishPath}'");
                    return;
                }

                var info = FileVersionInfo.GetVersionInfo(publishPath);
                string realV = VersionManager.CleanVersion(info.ProductVersion ?? info.FileVersion ?? "1.0.0");
                
                // PRE-FLIGHT INTEGRITY CHECK: Prevents uploading stale binaries
                string currentV = VersionManager.GetCurrentVersion();
                if (realV != currentV)
                {
                    AgregarLog($"âŒ ABORTANDO: Se detectÃ³ una inconsistencia crÃ­tica.");
                    AgregarLog($"Binario Destino: v{realV}");
                    AgregarLog($"Entorno Local:   v{currentV}");
                    AgregarLog("AsegÃºrate de haber guardado cambios en el .csproj y recompilado.");
                    return;
                }

                AgregarLog($"ðŸ›¡ï¸ Integridad verificada: Motor v{realV} listo para el Abismo.");

                // 3. Crear Release en GitHub
                string tag = $"v{realV}";
                AgregarLog($"â˜ Subiendo release '{tag}' a GitHub...");
                
                // Borrar release vieja si existe (opcional, pero ayuda a corregir errores de dedo)
                await RunCommand("gh", $"release delete {tag} -y --repo leaboga/nebula-modpack");
                
                int code = await RunCommand("gh", $"release create {tag} \"{publishPath}\" --repo leaboga/nebula-modpack --title \"KRAKEN Launcher v{realV}\" --notes \"ActualizaciÃ³n obligatoria del motor core.\"");
                
                if (code == 0)
                {
                    AgregarLog($"âœ… PublicaciÃ³n de MOTOR v{realV} completada.");
                    MessageBox.Show($"El Motor Core v{realV} ha sido desplegado.", "Kraken Update", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else AgregarLog($"âš  Fallo al subir a GitHub (CÃ³digo {code}).");
            }
            catch (Exception ex) { AgregarLog($"âŒ Error fatal en publicaciÃ³n de motor: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        private async void PublicarActualizacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("ðŸš€ Iniciando publicaciÃ³n de ASSETS (Mods/Configs)...");
            try
            {
                // 1. Determinar Nueva VersiÃ³n (SemVer Patch default)
                string currentV = _manifestActual?.Version ?? "1.0.0";
                string nextV    = VersionManager.Increment(currentV, VersionSegment.Patch);
                AgregarLog($"ðŸ“¦ Versionado de Assets: {currentV} âž” {nextV}");

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

                // 3. Crear Release en GitHub con Tag DinÃ¡mico
                string tag = $"v{nextV}-assets";
                AgregarLog($"â˜ Subiendo release '{tag}' a GitHub...");
                int code = await RunCommand("gh", $"release create {tag} \"{zipPath}\" --repo leaboga/nebula-modpack --title \"Assets v{nextV}\" --notes \"ActualizaciÃ³n automÃ¡tica de configuraciÃ³n y mods.\"");
                if (code != 0) { AgregarLog($"âš  Fallo al crear release (cÃ³digo {code}). VerificÃ¡ credenciales de gh."); return; }

                // 4. Sincronizar Repositorio de Manifiesto
                string tempRepo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kraken-repo-sync");
                if (Directory.Exists(tempRepo)) RobustDelete(tempRepo);
                await RunCommand("gh", $"repo clone leaboga/nebula-modpack \"{tempRepo}\"");

                // Crear nueva carpeta de versiÃ³n
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

                AgregarLog($"âœ… PublicaciÃ³n v{nextV} completada satisfactoriamente.");
                MessageBox.Show($"La versiÃ³n {nextV} ha sido desplegada en el enjambre.", "Ã‰xito GalÃ¡ctico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"âŒ Error fatal en publicaciÃ³n: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        //  HELPERS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
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
                AgregarLog($"âš  RobustDelete (Fase 1): {ex.Message}. Intentando desintegraciÃ³n forzada..."); 
                try
                {
                    // Fallback agresivo para locks de Windows (Memoria de Usuario)
                    var psi = new ProcessStartInfo("cmd.exe", $"/c rd /s /q \"{path}\"") { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(psi)?.WaitForExit();
                    if (Directory.Exists(path)) AgregarLog("âŒ Error: La carpeta resiste la eliminaciÃ³n forzada.");
                }
                catch (Exception ex2) { AgregarLog($"âš  RobustDelete (Fase 2): {ex2.Message}"); }
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
                catch (Exception ex) { AgregarLog($"âš  RunCommand({cmd}): {ex.Message}"); return -1; }
            });
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string f in Directory.GetFiles(source)) try { File.Copy(f, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(f)), true); } catch { }
            foreach (string d in Directory.GetDirectories(source)) CopyDirectory(d, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(d)));
        }
        // â”€â”€ Performance & HW â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                AgregarLog("ðŸš€ Prioridad de proceso establecida en ALTA.");
            } catch { }
        }

        // â”€â”€ UI Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void AnimateView(FrameworkElement element)
        {
            element.Visibility = Visibility.Visible;
            var sb = (Storyboard)FindResource("TabChangeEffect");
            sb.Begin(element);
        }

        public void CambiarVista(string vista) => NavigationService.Instance.NavigateTo(vista, this);

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
                AgregarLog("ðŸ§¹ CachÃ© y archivos temporales eliminados con Ã©xito.");
                MessageBox.Show("CachÃ© y archivos temporales limpiados.", "Limpieza completada", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) { AgregarLog($"âŒ Error limpiando cachÃ©: {ex.Message}"); }
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
