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
using KrakenLauncher.Services;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Documents;
using KrakenLauncher.Modules;

namespace KrakenLauncher
{

    public partial class MainWindow : Window
    {
        // Ã¢â€â‚¬Ã¢â€â‚¬ Paths Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        public MinecraftProfile? CurrentProfile => _session.Profiles.Find(p => p.Id == _session.CurrentProfileId) ?? (_session.Profiles.Count > 0 ? _session.Profiles[0] : null);
        public string GameFolder => PathService.GetInstanceFolder(CurrentProfile?.Id ?? "default");

        // Ã¢â€â‚¬Ã¢â€â‚¬ Theme brushes Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        private static readonly SolidColorBrush BrushOnline  = new(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly SolidColorBrush BrushOffline = new(Color.FromRgb(0xEF, 0x44, 0x44));


        private const string UpdateCheckUrl = "https://api.github.com/repos/leaboga/nebula-modpack/releases/latest";
        
        // Ã¢â€â‚¬Ã¢â€â‚¬ Services Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        private readonly SocialService          _socialService    = new();
        private readonly ServerStatusCache      _cache            = new();
        private readonly ChangelogService       _changelogService = new();
        private readonly SkinService            _skinService      = new();
        private readonly SessionHistoryService  _historyService   = new();
        private readonly DiscordRPCService      _discord          = new();
        private BackupService                   _backupService    = null!;
        private CrashReporterService            _crashReporter    = null!;
        private ModSyncer                       _syncer           = null!;

        // Ã¢â€â‚¬Ã¢â€â‚¬ State Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  WINDOW CONTROLS
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SidebarColumn == null || ActionBarRow == null) return;

            if (ActualWidth < 860)
            {
                SidebarColumn.Width = new GridLength(176);
                ActionBarRow.Height = new GridLength(138);
            }
            else if (ActualWidth < 980)
            {
                SidebarColumn.Width = new GridLength(204);
                ActionBarRow.Height = new GridLength(128);
            }
            else
            {
                SidebarColumn.Width = new GridLength(230);
                ActionBarRow.Height = new GridLength(120);
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

        // Ã¢â€â‚¬Ã¢â€â‚¬ Particles Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        private readonly List<(Ellipse dot, double vx, double vy)> _particles = new();
        private DispatcherTimer? _particleTimer;
        private readonly Random _rnd = new();

        // Ã¢â€â‚¬Ã¢â€â‚¬ Notifications (friend tracking) Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
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

                if (!_session.HasFinishedDiscovery)
                {
                    Dispatcher.InvokeAsync(async () => {
                        await Task.Delay(1000);
                        StartDiscovery();
                    });
                }
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
                    AgregarLog($"Ã°Å¸â€ºÂ¡Ã¯Â¸Â Sistema Operativo Kraken v{liveVersion} Ã¢â‚¬â€ NÃƒÂºcleo estable.");
                    
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
        }// Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  PARTICLES
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  SERVER STATUS + FRIEND NOTIFICATIONS
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                    AgregarLog($"Ã°Å¸â€˜â€¹ {p} se ha unido al servidor.");
                    // Mejora: Toast feedback visual rÃƒÂ¡pido
                    Dispatcher.Invoke(() => {
                        StatusText.Text = $"Ã¢Å“Â¨ {p} entrÃƒÂ³!";
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  LAUNCHER UPDATE CHECK
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private string? _updateDownloadUrl;
        private string? _updateVersion;
        private string? _autoUpdateAttemptedVersion;
        private bool _isAutoUpdating;

        private async Task CheckForLauncherUpdate()
        {
            try
            {
                AgregarLog("ðŸ” Verificando integridad del nÃºcleo Kraken...");
                string localV = VersionManager.GetCurrentVersion();
                string currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                using var http = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher");
                
                var resRels = await http.GetStringAsync("https://api.github.com/repos/leaboga/nebula-modpack/releases?per_page=10");
                var releases = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic[]>(resRels);
                if (releases == null || releases.Length == 0) return;
                dynamic? latestSoftware = null;
                foreach (var r in releases) { if (!(r.tag_name?.ToString() ?? "").Contains("-assets")) { latestSoftware = r; break; } }
                if (latestSoftware == null) return;
                var root = latestSoftware;

                string remoteTag = root.tag_name?.ToString() ?? "";
                string remoteV   = VersionManager.CleanVersion(remoteTag);
                
                AgregarLog($"Ã°Å¸â€Â AuditorÃƒÂ­a de ActualizaciÃƒÂ³n: Local={localV} | Remota={remoteV}");

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

                string changelog = root.name?.ToString() ?? "Nueva versiÃƒÂ³n disponible";
                
                _updateDownloadUrl = null;
                string selectedAssetName = string.Empty;
                string currentExeName = System.IO.Path.GetFileName(Environment.ProcessPath ?? "KrakenLauncher.exe");
                string[] preferredAssetNames = new[]
                {
                    currentExeName,
                    "KrakenLauncher.exe",
                    "KrakenLauncher.exe"
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
                    AgregarLog("Ã¢Å¡Â  No se encontrÃƒÂ³ un binario (.exe) vÃƒÂ¡lido en la release remota. Abortando update.");
                    return;
                }

                _updateVersion = remoteV;
                UpdateDiagnosticsService.MarkCheck(localV, remoteV, selectedAssetName, _updateDownloadUrl, currentExePath);

                Dispatcher.Invoke(() =>
                {
                    UpdateBadge.Text       = "Ã¢Å¡Â¡ NUEVA CORE v" + remoteV;
                    UpdateBadge.Visibility = Visibility.Visible;
                    UpdateBadge.ToolTip    = $"Detectada v{remoteV}: " + changelog;
                    UpdateBadge.IsEnabled  = false;
                    AgregarLog($"Ã¢Å“Â¨ [ActualizaciÃƒÂ³n] Kraken v{remoteV} detectado. Se inicia la auto-actualizaciÃƒÂ³n.");
                });

                if (!_isAutoUpdating && _autoUpdateAttemptedVersion != remoteV)
                {
                    _autoUpdateAttemptedVersion = remoteV;
                    _isAutoUpdating = true;
                    await AplicarUpdateAsync(_updateDownloadUrl, true);
                }
            }
            catch (Exception ex) { AgregarLog("Ã¢Å¡Â  Error en auditorÃƒÂ­a de versiÃƒÂ³n: " + ex.Message); }
        }

        private async void UpdateBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrEmpty(_updateVersion)) return;

            var result = MessageBox.Show(
                "Nueva version disponible: v" + _updateVersion + "\n\n" +
                UpdateBadge.ToolTip + "\n\n" +
                "El launcher se descargarÃƒÂ¡ y reiniciarÃƒÂ¡. Ã‚Â¿Continuar?",
                "ActualizaciÃƒÂ³n",
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
                string targetExe = currentExe;
                string downloadedAssetName = System.IO.Path.GetFileName(targetExe);
                
                AgregarLog("ðŸ“¡ Iniciando descarga del nÃºcleo v" + _updateVersion + "...");
                
                using var http = new HttpClient() { Timeout = TimeSpan.FromMinutes(10) };
                http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher");

                string updateDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KrakenUpdate_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(updateDir);
                string tempExe = System.IO.Path.Combine(updateDir, downloadedAssetName);

                using (var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalSize = response.Content.Headers.ContentLength;
                    using (var fs = new FileStream(tempExe, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        byte[] buffer = new byte[81920]; // 80 KB
                        long totalRead = 0;
                        int read;
                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            
                            // Log only major milestones to avoid spamming
                            if (totalSize.HasValue && totalRead % (20 * 1024 * 1024) < 81920) 
                            {
                                int pct = (int)((double)totalRead / totalSize.Value * 100);
                                AgregarLog($"ðŸ“¥ Descargando core: {pct}% completado...");
                            }
                        }
                    }
                }

                AgregarLog($"âœ… Descarga lista. Iniciando secuencia de reinicio...");
                
                UpdateDiagnosticsService.MarkApplying(targetExe, isAutomatic);

                int pid = Process.GetCurrentProcess().Id;
                string batContent = "@echo off\n" +
                                   "title Kraken Core Updater\n" +
                                   "echo [UPDATE] Aguardando el cierre de procesos activos...\n" +
                                   $"taskkill /F /PID {pid} > nul 2>&1\n" +
                                   "timeout /t 2 /nobreak > nul\n" +
                                   "set /a count=0\n" +
                                   ":loop\n" +
                                   "set /a count+=1\n" +
                                   "echo [UPDATE] Intento de reemplazo %count% de 10...\n" +
                                   "echo [%date% %time%] Intento %count%: copy \"" + tempExe + "\" -> \"" + targetExe + "\">> \"" + PathService.UpdaterLogFile + "\"\n" +
                                   "copy /Y \"" + tempExe + "\" \"" + targetExe + "\"\n" +
                                   "if errorlevel 1 (\n" +
                                   "    echo [WARN] Archivo bloqueado. Reintentando en 2s...\n" +
                                   "    if %count% geq 10 goto failed\n" +
                                   "    timeout /t 2 /nobreak > nul\n" +
                                   "    goto loop\n" +
                                   ")\n" +
                                   "echo [UPDATE] Motor actualizado con Ã©xito. Reiniciando...\n" +
                                   "echo [%date% %time%] EXITO: Nucleo actualizado. >> \"" + PathService.UpdaterLogFile + "\"\n" +
                                   "start \"\" \"" + targetExe + "\"\n" +
                                   "rmdir /s /q \"" + updateDir + "\"\n" +
                                   "exit\n" +
                                   ":failed\n" +
                                   "echo [ERROR] No se pudo sobrescribir el motor galÃ¡ctico. >> \"" + PathService.UpdaterLogFile + "\"\n" +
                                   "echo [ERROR] El archivo sigue bloqueado por otro proceso.\n" +
                                   "pause\n" +
                                   "exit\n";

                string updaterBat = System.IO.Path.Combine(updateDir, "kraken_updater.bat");
                await File.WriteAllTextAsync(updaterBat, batContent);

                AgregarLog("ðŸš€ Reiniciando para aplicar la actualizaciÃ³n...");

                Process.Start(new ProcessStartInfo("cmd.exe", "/C \"" + updaterBat + "\"")
                {
                    UseShellExecute = true,
                    CreateNoWindow = false
                });

                UpdateDiagnosticsService.MarkRestartScheduled();
                _cerrarDeVerdad = true;
                Dispatcher.Invoke(() => Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                _isAutoUpdating = false;
                AgregarLog("âš ï¸ Error al aplicar actualizaciÃ³n: " + ex.Message);
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  SKIN
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  SESSION HISTORY
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â

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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  NAVIGATION
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private void Nav_Home_Checked(object sender, RoutedEventArgs e)       { CambiarVista("home"); }
        private void Nav_Sistemas_Checked(object sender, RoutedEventArgs e)   { CambiarVista("sistemas"); }
        private void Nav_Recursos_Checked(object sender, RoutedEventArgs e)   { CambiarVista("recursos"); }
        private void Nav_Red_Checked(object sender, RoutedEventArgs e)        { CambiarVista("red"); }
        
        private void MapQuickCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                CambiarVista("red");
            }
        }

        private void StopCurrentModule()
        {
            try
            {
                if (ModulesContainer?.Content is SocialView      sv) sv.Stop();
                if (ModulesContainer?.Content is PerformanceView pv) pv.Stop();
                if (ModulesContainer?.Content is HubView hv) hv.StopActive();
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  COPY IP
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  BACKUP
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private async void BackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn       = (Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "Ã¢ÂÂ³ Creando backup...";
            try
            {
                string path = await _backupService.CreateBackupAsync(msg => AgregarLog(msg));
                MessageBox.Show($"Backup creado exitosamente:\n{System.IO.Path.GetFileName(path)}",
                                "Backup completado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"Ã¢ÂÅ’ Error en backup: {ex.Message}"); }
            finally { btn.IsEnabled = true; btn.Content = "Ã°Å¸â€™Â¾ Crear Backup Ahora"; }
        }

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  LOG
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                    if (mensaje.StartsWith("Ã¢Å“â€¦") || mensaje.StartsWith("Ã¢Å“â€œ")) runText.Foreground = Brushes.LightGreen;
                    else if (mensaje.StartsWith("Ã¢ÂÅ’") || mensaje.StartsWith("Ã¢Å“â€”") || mensaje.Contains("Error")) runText.Foreground = Brushes.Salmon;
                    else if (mensaje.StartsWith("Ã¢Å¡Â Ã¯Â¸Â") || mensaje.Contains("Warning")) runText.Foreground = Brushes.Gold;
                    else if (mensaje.StartsWith("Ã°Å¸Å¡â‚¬") || mensaje.StartsWith("Ã¢Å¡Â¡")) runText.Foreground = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
                    else runText.Foreground = new SolidColorBrush(Color.FromRgb(0xC4, 0xB5, 0xFD));

                    if (LogText.Text == "[KRAKEN] System initialized. Waiting for command...") LogText.Inlines.Clear();
                    
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  SESSION PERSISTENCE
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                var defaultProfile = new MinecraftProfile { Name = "KRAKEN Default", Version = "1.21.1", LoaderType = "vanilla" };
                _session.Profiles.Add(defaultProfile);
                _session.CurrentProfileId = defaultProfile.Id;
            }
            if (string.IsNullOrEmpty(_session.CurrentProfileId)) _session.CurrentProfileId = _session.Profiles[0].Id;

            // MigraciÃ³n de nombres
            _session.Profiles.ForEach(p => { 
                if (p.Name.Contains("Nebula Default")) p.Name = "KRAKEN Default";
                if (p.RamGB < 2) p.RamGB = 4; 
            });

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
            string greeting = hour < 12 ? "Buenos dÃƒÂ­as" : hour < 19 ? "Buenas tardes" : "Buenas noches";
            
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
                ? $"{greeting}, {_session.Username} Ã°Å¸â€˜â€¹\nÃ°Å¸â€œÂ¢ {currentNews}"
                : "Listo para jugar";
        }

        private void MostrarUsuarioPremium(string username)
        {
            if (LoggedUsernameText != null) LoggedUsernameText.Text      = username;
            if (NotLoggedPanel     != null) NotLoggedPanel.Visibility    = Visibility.Collapsed;
            if (LoggedPanel        != null) LoggedPanel.Visibility       = Visibility.Visible;
            ActualizarSidebar();
        }

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  UI EVENTS
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
            AgregarLog($"Ã°Å¸â€œâ€š Perfil cambiado a: {_session.Profiles[idx].Name}");
            
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
                var p = new MinecraftProfile { Name = "KRAKEN Default", Version = "1.21.1", LoaderType = "vanilla" };
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
            
            AgregarLog($"Ã°Å¸â€”â€˜Ã¯Â¸Â Perfil '{profileToDelete.Name}' eliminado.");
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddProfileWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultProfile != null)
            {
                var p = dialog.ResultProfile;
                _session.Profiles.Add(p);
                _session.CurrentProfileId = p.Id;
                GuardarSesion();
                ActualizarComboPerfiles();
                AgregarLog($"âœ… Perfil '{p.Name}' ({p.Version} {p.LoaderType}) creado.");
            }
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
            AgregarLog($"Ã¢Å“â€¦ Perfil '{newName}' clonado con ÃƒÂ©xito.");
        }

        private void VerLog_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(PathService.LogFile)) { AgregarLog("Ã¢â€žÂ¹Ã¯Â¸Â No hay log guardado aÃƒÂºn."); return; }
            try { Process.Start(new ProcessStartInfo { FileName = "notepad.exe", Arguments = $"\"{PathService.LogFile}\"", UseShellExecute = true }); }
            catch (Exception ex) { AgregarLog($"Ã¢Å¡Â Ã¯Â¸Â Error abriendo log: {ex.Message}"); }
        }

        private async void RepararModpack_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) { btn.IsEnabled = false; btn.Content = "Ã¢ÂÂ³ Reparando..."; }
            try
            {
                await SincronizarTodoAsync();
                AgregarLog("Ã¢Å“â€¦ SincronizaciÃƒÂ³n completada.");
                MessageBox.Show("SincronizaciÃƒÂ³n y reparaciÃƒÂ³n completada con ÃƒÂ©xito.", "KRAKEN Launcher", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"Ã¢ÂÅ’ Error en reparaciÃƒÂ³n: {ex.Message}"); }
            finally { if (btn != null) { btn.IsEnabled = true; btn.Content = "Ã°Å¸â€ºÂ Ã¯Â¸Â Reparar Pack"; } }
        }

        public async Task SincronizarTodoAsync()
        {
            if (CurrentProfile == null) return;
            AgregarLog("Ã°Å¸â€ºÂ Ã¯Â¸Â Iniciando sincronizaciÃƒÂ³n total (GitHub)...");
            
            _manifestActual = null; // Force reload from server
            await CargarVersionesAsync();
            
            if (_manifestActual != null)
            {
                // 1. Sync MODS
                PlayButton.Content = "Sincronizando mods...";
                bool modsOk = await _syncer.SincronizarMods(_manifestActual);
                
                // 2. Sync CONFIGS/ASSETS
                PlayButton.Content = "Actualizando configs...";
                await _syncer.SincronizarConfigs(_manifestActual?.Version);
                MandatoryFixesService.ApplyToKnownClientFolders(GameFolder, AgregarLog);
                
                if (modsOk)
                {
                    CurrentProfile.LastSyncDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    CurrentProfile.LastSyncHash = _manifestActual.Version;
                    GuardarSesion();
                    AgregarLog($"Ã¢Å“â€œ Perfil '{CurrentProfile.Name}' sincronizado correctamente.");
                }
            }
            else
            {
                AgregarLog("Ã¢Å¡Â  No se pudo obtener el manifiesto de GitHub.");
            }
            
            PlayButton.Content = "Ã¢â€“Â¶  JUGAR";
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
                // Seguridad de Admin: Solo Pepita/Leandro en SU mÃ¡quina o con flag Admin
                bool esAdminPc = Environment.MachineName.Equals("LEANDRO-PC", StringComparison.OrdinalIgnoreCase);
                bool esPepita = (_session.IsAdmin && esAdminPc)
                             || (_session.Username.Equals("Pepita",  StringComparison.OrdinalIgnoreCase) && esAdminPc)
                             || (_session.Username.Equals("Leandro", StringComparison.OrdinalIgnoreCase) && esAdminPc);

                var remoteInfo = await _syncer.ObtenerHashConfigsRemoto();
                if (remoteInfo == null || string.IsNullOrEmpty(remoteInfo.Value.hash))
                {
                    AgregarLog("Info: No se pudo verificar configs de Pepita (sin conexiÃ³n).");
                    return;
                }

                string hashRemoto = remoteInfo.Value.hash;
                int? recommendedRam = remoteInfo.Value.ram;
                string? officialJvmArgs = remoteInfo.Value.jvmArgs;
                recommendedRam = null; // La RAM la decide cada jugador; solo se usa como dato informativo remoto.
                string profileId = CurrentProfile?.Id ?? "default";

                bool hayNuevasConfigs = hashRemoto != _session.LastAppliedConfigHash;
                if (!hayNuevasConfigs)
                {
                    AgregarLog("Configs al dÃ­a (sin cambios de Pepita).");
                    return;
                }

                // Si ya rechazÃ³ esta versiÃ³n especÃ­fica de hash, no volver a preguntar hasta que cambie el hash
                if (!forzar && _session.RejectedConfigVersions.ContainsKey(profileId) && _session.RejectedConfigVersions[profileId] == hashRemoto)
                {
                    AgregarLog("Aviso: Hay configs de Pepita nuevas, pero ya las rechazaste anteriormente.");
                    return;
                }

                if (esPepita && !forzar)
                {
                    AgregarLog("Pepita: hay configs nuevas publicadas. PodÃ©s aplicarlas desde el panel Config.");
                    return;
                }

                bool aplicar = forzar;
                if (!forzar)
                {
                    var resultado = Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            "Hay una nueva configuracion oficial del modpack.\n\n" +
                            "Se aplican options.txt, config/, shaderpacks/ y argumentos JVM seguros.\n" +
                            "Tu RAM dedicada se conserva: cada jugador la elige desde su perfil.\n\n" +
                            "Si elegis que no, no volveras a ver este mensaje hasta la proxima actualizacion.",
                            "Config oficial disponible",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question));
                    aplicar = resultado == MessageBoxResult.Yes;
                }

                if (aplicar)
                {
                    AgregarLog("Aplicando configs integrales de Pepita...");
                    
                    // SobreescribirTodo = true para que incluya options.txt y todo lo de Pepita
                    await _syncer.SincronizarConfigs(hashRemoto, sobrescribirTodo: true);
                    MandatoryFixesService.ApplyToKnownClientFolders(GameFolder, AgregarLog);
                    _session.LastAppliedConfigHash = hashRemoto;

                    if (!string.IsNullOrWhiteSpace(officialJvmArgs) && CurrentProfile != null)
                    {
                        CurrentProfile.JvmArgs = string.Join(' ', McGameLauncher.ParseJvmArgs(officialJvmArgs));
                        AgregarLog("Argumentos JVM oficiales aplicados. La RAM personal se conserva.");
                    }
                    
                    // Aplicar RAM recomendada si viene en el hash
                    if (recommendedRam.HasValue && CurrentProfile != null)
                    {
                        CurrentProfile.RamGB = recommendedRam.Value;
                        AgregarLog($"ðŸš€ RAM ajustada a la recomendada: {recommendedRam.Value}GB");
                    }

                    _session.RejectedConfigVersions.Remove(profileId);
                    GuardarSesion();
                    Services.NotificationService.Instance.ShowSuccess("Ajustes de Pepita aplicados al 100%.");
                }
                else
                {
                    // Guardar el hash actual como rechazado para no volver a molestar
                    _session.RejectedConfigVersions[profileId] = hashRemoto;
                    GuardarSesion();
                    AgregarLog("Configs de Pepita omitidas. No se volverÃ¡ a preguntar para esta versiÃ³n.");
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  VERSIONS
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private async Task CargarVersionesAsync()
        {
            try
            {
                AgregarLog("\uD83D\uDD0D Verificando versiones disponibles...");
                _versionsIndex = await _syncer.ObtenerVersionsIndex();

                if (_versionsIndex?.AvailableVersions == null || _versionsIndex.AvailableVersions.Count == 0)
                { AgregarLog("Ã¢Å¡Â Ã¯Â¸Â No se pudieron cargar las versiones."); return; }

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

                // --- NUEVO: VerificaciÃ³n de Config Oficial ---
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
                            $"Ã¢Å“Â¨ Hay una nueva configuraciÃƒÂ³n oficial v{versionOficial} disponible para este perfil.\n\n" +
                            "Incluye optimizaciones de rendimiento, shaders y keybinds recomendados.\n" +
                            "Ã‚Â¿Deseas aplicarla ahora?\n\n" +
                            "(Tus controles personales serÃƒÂ¡n respetados)",
                            "ConfiguraciÃƒÂ³n Recomendada",
                            MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (res == MessageBoxResult.Yes)
                        {
                            _ = AplicarConfigOficialAsync(manifest);
                        }
                        else if (res == MessageBoxResult.No)
                        {
                            // Guardar rechazo para no volver a molestar con ESTA versiÃƒÂ³n
                            _session.RejectedConfigVersions[profileId] = versionOficial;
                            GuardarSesion();
                            AgregarLog($"Ã°Å¸â€â€ Config oficial v{versionOficial} rechazada por el usuario.");
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
                AgregarLog($"Ã°Å¸â€â€ž Aplicando configuraciÃƒÂ³n oficial v{manifest.ConfigVersion}...");
                
                // Backup simple
                string backupDir = System.IO.Path.Combine(GameFolder, "backups", "auto-config-v" + manifest.ConfigVersion);
                Directory.CreateDirectory(backupDir);
                foreach (var target in new[] { "options.txt", "optionsshaders.txt", "config", "shaderpacks" })
                {
                    string src = System.IO.Path.Combine(GameFolder, target);
                    if (File.Exists(src)) File.Copy(src, System.IO.Path.Combine(backupDir, target), true);
                    else if (Directory.Exists(src)) CopyDirectory(src, System.IO.Path.Combine(backupDir, target));
                }

                await _syncer.SincronizarConfigs(manifest.Version, sobrescribirTodo: true);
                MandatoryFixesService.ApplyToKnownClientFolders(GameFolder, AgregarLog);

                var remoteConfig = await _syncer.ObtenerHashConfigsRemoto();
                if (CurrentProfile != null && !string.IsNullOrWhiteSpace(remoteConfig?.jvmArgs))
                    CurrentProfile.JvmArgs = string.Join(' ', McGameLauncher.ParseJvmArgs(remoteConfig.Value.jvmArgs));

                string profileId = CurrentProfile?.Id ?? "default";
                _session.AppliedConfigVersions[profileId] = manifest.ConfigVersion;
                _session.RejectedConfigVersions.Remove(profileId);
                GuardarSesion();

                AgregarLog($"Ã¢Å“â€¦ ConfiguraciÃƒÂ³n oficial v{manifest.ConfigVersion} aplicada correctamente.");
                NotificationService.Instance.ShowSuccess($"Config oficial v{manifest.ConfigVersion} lista.");
            }
            catch (Exception ex) { AgregarLog($"Ã¢Å¡Â  Error aplicando config oficial: {ex.Message}"); }
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  AUTH
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                else AgregarLog("Ã¢Å¡Â Ã¯Â¸Â La autenticaciÃƒÂ³n no devolviÃƒÂ³ sesiÃƒÂ³n vÃƒÂ¡lida.");
            }
            catch (Exception ex)
            {
                AgregarLog($"Ã¢ÂÅ’ Error en login Microsoft: {ex.Message}");
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

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  ADMIN
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                AgregarLog($"Ã°Å¸â€œâ€š Instancia sincronizada con el perfil activo.");
                _ = CargarVersionesAsync();
            }
            catch (Exception ex) { AgregarLog($"\u274C Error al cambiar instancia: {ex.Message}"); }
        }

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  PLAY
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_session.Username))
            { AgregarLog("Ã¢Å¡Â Ã¯Â¸Â Ingresa un nombre de usuario."); MessageBox.Show("Ingresa un nombre primero.", "Sin usuario", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            if (_session.Username.Length < 3) { AgregarLog("Ã¢Å¡Â Ã¯Â¸Â El nombre debe tener al menos 3 caracteres."); return; }
            if (CurrentProfile == null) { AgregarLog("\u26A0 No hay perfil seleccionado."); return; }

            ImportarConfigsDeMinecraftOriginal();
            PlayButton.IsEnabled = false;
            PlayButton.Content   = "Iniciando...";

            var sessionStart = DateTime.Now;

            try
            {
                bool turboMode = _session.IsTurboEnabled || Keyboard.IsKeyDown(Key.LeftShift);

                bool isNewInstall = !Directory.Exists(System.IO.Path.Combine(GameFolder, "versions")) || 
                                    !Directory.Exists(System.IO.Path.Combine(GameFolder, "mods")) ||
                                    Directory.GetFiles(System.IO.Path.Combine(GameFolder, "mods"), "*.jar").Length == 0;

                if (isNewInstall)
                {
                    AgregarLog("ðŸ“¡ Perfil vacÃ­o detectado. Iniciando configuraciÃ³n inicial completa...");
                    turboMode = false;
                }
                
                // BACKUP AUTOMATICO (SEGURIDAD PRIMERO)
                if (!turboMode)
                {
                    AgregarLog("Ã°Å¸â€™Â¾ Creando backup de seguridad (rÃƒÂ¡pido)...");
                    await _backupService.CreateQuickConfigBackupAsync();
                }

                if (turboMode) AgregarLog("Ã¢Å¡Â¡ Modo Turbo activado Ã¢â‚¬â€ omitiendo sincronizaciÃƒÂ³n de archivos.");

                if (!turboMode && _manifestActual != null)
                {
                    PlayButton.Content = "Sincronizando mods...";
                    _discord.SetActivity("Sincronizando mods...");
                    bool modsOk = await _syncer.SincronizarMods(_manifestActual);
                    if (!modsOk) { AgregarLog("Ã¢ÂÅ’ FallÃƒÂ³ la descarga de mods."); return; }

                    // --- CONFIGS DE PEPITA: verificar hash remoto ---
                    PlayButton.Content = "Verificando configs...";
                    
                    if (isNewInstall)
                    {
                        AgregarLog("ðŸ“¥ Descargando activos y configuraciones base...");
                        await _syncer.SincronizarConfigs(_manifestActual.Version, sobrescribirTodo: true);
                        MandatoryFixesService.ApplyToKnownClientFolders(GameFolder, AgregarLog);
                        var res = await _syncer.ObtenerHashConfigsRemoto();
                        _session.LastAppliedConfigHash = res?.hash;
                        if (CurrentProfile != null && !string.IsNullOrWhiteSpace(res?.jvmArgs))
                            CurrentProfile.JvmArgs = string.Join(' ', McGameLauncher.ParseJvmArgs(res.Value.jvmArgs));
                        GuardarSesion();
                    }
                    else
                    {
                        await AplicarConfigsSiHayCambiosAsync(forzar: false);
                    }
                }
                MandatoryFixesService.ApplyToKnownClientFolders(GameFolder, AgregarLog);
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
                ProgressLabel.Text = exitCode == 0 ? "SesiÃƒÂ³n finalizada." : $"Minecraft cerrÃƒÂ³ con cÃƒÂ³digo {exitCode}.";

                // Record session
                var duration = DateTime.Now - sessionStart;
                if (duration.TotalMinutes >= 1) { _historyService.RecordSession(duration); ActualizarSessionHistoryUI(); }

                // Cloud Sync
                if (!string.IsNullOrEmpty(_session.CloudPath))
                {
                    AgregarLog("Ã¢ËœÂÃ¯Â¸Â Iniciando respaldo en la nube...");
                    string zip = await _backupService.CreateBackupAsync();
                    await _backupService.CopyToCloudAsync(zip, _session.CloudPath, msg => AgregarLog(msg));
                }

                // Check for crashes (Professional Insight)
                var analysis = _crashReporter.AnalyzeLastCrash(sessionStart);
                if (analysis != null)
                {
                    AgregarLog("Ã°Å¸â€™Â¥ Crash detectado. Mostrando diagnÃƒÂ³stico Nebula...");
                    SwitchToModule(new CrashAnalysisView(analysis, GameFolder));
                    
                    // Auto-report to Discord if configured
                    if (!string.IsNullOrEmpty(_session.CrashWebhookUrl))
                    {
                        string summary = _crashReporter.CheckForCrash(sessionStart) ?? "Error descriptivo no disponible.";
                        await _crashReporter.ReportToDiscordAsync(summary, _session.Username);
                        AgregarLog("Ã¢Å“â€¦ Crash reportado al servidor automÃƒÂ¡ticamente.");
                    }
                }

                _discord.SetIdle();
            }
             catch (Exception ex) { AgregarLog($"Ã¢Å“â€” Error: {ex.Message}"); Show(); }
            finally { PlayButton.IsEnabled = true; PlayButton.Content = "Ã¢â€“Â¶  JUGAR"; }
        }

        // Removed old simple log analyzer, now using CrashReporterService.CrashAnalysis

        private async Task<int> LanzarMinecraft(MinecraftProfile profile)
        {
            // VerificaciÃƒÂ³n de Conflictos (Imp 11)
            VerificarConflictosDeMods();

            var mcLauncher = new McGameLauncher(GameFolder, profile.RamGB, _session.Username,
                _session.AuthMode == "premium", profile.Version, 
                profile.LoaderType,
                profile.LoaderVersion, 
                manualJavaPath: profile.JavaPath,
                customSplash: _session.CustomSplashText,
                isOverlay: _session.IsOverlayEnabled,
                jvmArgs: profile.JvmArgs);
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
                        AgregarLog("Ã¢Å¡Â  Conflicto detectado: Rubidium y Embeddium juntos causan crash.");
                        MessageBox.Show("Se detectÃƒÂ³ un conflicto entre Rubidium y Embeddium.\nEjecutÃƒÂ¡ 'Reparar Pack' para una instalaciÃƒÂ³n limpia.", "Conflicto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            } catch { }
        }

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  ADMIN Ã¢â‚¬â€ PUBLISH UPDATE
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        private async void PublicarLauncher_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("Ã°Å¸Å¡â‚¬ Iniciando publicaciÃƒÂ³n de MOTOR CORE...");
            try
            {
                // 1. Rebuild en modo Release
                AgregarLog("Ã°Å¸â€Â¨ Compilando binario final (Release)...");
                int buildResult = await RunCommand("dotnet", "publish KrakenLauncher.csproj -c Release -r win-x64 --self-contained true");
                if (buildResult != 0) { AgregarLog("Ã¢ÂÅ’ Error: FallÃƒÂ³ la compilaciÃƒÂ³n del motor."); return; }

                // 2. Extraer versiÃƒÂ³n REAL del binario generado
                string publishPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "bin", "Release", "net8.0-windows", "win-x64", "publish", "KrakenLauncher.exe");
                
                // Fallback attempt to find the publish folder
                if (!File.Exists(publishPath))
                    publishPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "bin", "Release", "net8.0-windows", "win-x64", "publish", "KrakenLauncher.exe");

                if (!File.Exists(publishPath))
                {
                    AgregarLog($"Ã¢ÂÅ’ Error: No se encontrÃƒÂ³ el binario en '{publishPath}'");
                    return;
                }

                var info = FileVersionInfo.GetVersionInfo(publishPath);
                string realV = VersionManager.CleanVersion(info.ProductVersion ?? info.FileVersion ?? "1.0.0");
                
                // PRE-FLIGHT INTEGRITY CHECK: Prevents uploading stale binaries
                string currentV = VersionManager.GetCurrentVersion();
                if (realV != currentV)
                {
                    AgregarLog($"Ã¢ÂÅ’ ABORTANDO: Se detectÃƒÂ³ una inconsistencia crÃƒÂ­tica.");
                    AgregarLog($"Binario Destino: v{realV}");
                    AgregarLog($"Entorno Local:   v{currentV}");
                    AgregarLog("AsegÃƒÂºrate de haber guardado cambios en el .csproj y recompilado.");
                    return;
                }

                AgregarLog($"Ã°Å¸â€ºÂ¡Ã¯Â¸Â Integridad verificada: Motor v{realV} listo para el Abismo.");

                // 3. Crear Release en GitHub
                string tag = $"v{realV}";
                AgregarLog($"Ã¢ËœÂ Subiendo release '{tag}' a GitHub...");
                
                // Borrar release vieja si existe (opcional, pero ayuda a corregir errores de dedo)
                await RunCommand("gh", $"release delete {tag} -y --repo leaboga/nebula-modpack");
                
                int code = await RunCommand("gh", $"release create {tag} \"{publishPath}\" --repo leaboga/nebula-modpack --title \"KRAKEN Launcher v{realV}\" --notes \"ActualizaciÃƒÂ³n obligatoria del motor core.\"");
                
                if (code == 0)
                {
                    AgregarLog($"Ã¢Å“â€¦ PublicaciÃƒÂ³n de MOTOR v{realV} completada.");
                    MessageBox.Show($"El Motor Core v{realV} ha sido desplegado.", "Kraken Update", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else AgregarLog($"Ã¢Å¡Â  Fallo al subir a GitHub (CÃƒÂ³digo {code}).");
            }
            catch (Exception ex) { AgregarLog($"Ã¢ÂÅ’ Error fatal en publicaciÃƒÂ³n de motor: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        private async void PublicarActualizacion_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender; btn.IsEnabled = false;
            AgregarLog("Ã°Å¸Å¡â‚¬ Iniciando publicaciÃƒÂ³n de ASSETS (Mods/Configs)...");
            try
            {
                // 1. Determinar Nueva VersiÃƒÂ³n (SemVer Patch default)
                string currentV = _manifestActual?.Version ?? "1.0.0";
                string nextV    = VersionManager.Increment(currentV, VersionSegment.Patch);
                AgregarLog($"Ã°Å¸â€œÂ¦ Versionado de Assets: {currentV} Ã¢Å¾â€ {nextV}");

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

                // 3. Crear Release en GitHub con Tag DinÃƒÂ¡mico
                string tag = $"v{nextV}-assets";
                AgregarLog($"Ã¢ËœÂ Subiendo release '{tag}' a GitHub...");
                int code = await RunCommand("gh", $"release create {tag} \"{zipPath}\" --repo leaboga/nebula-modpack --title \"Assets v{nextV}\" --notes \"ActualizaciÃƒÂ³n automÃƒÂ¡tica de configuraciÃƒÂ³n y mods.\"");
                if (code != 0) { AgregarLog($"Ã¢Å¡Â  Fallo al crear release (cÃƒÂ³digo {code}). VerificÃƒÂ¡ credenciales de gh."); return; }

                // 4. Sincronizar Repositorio de Manifiesto
                string tempRepo = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kraken-repo-sync");
                if (Directory.Exists(tempRepo)) RobustDelete(tempRepo);
                await RunCommand("gh", $"repo clone leaboga/nebula-modpack \"{tempRepo}\"");

                // Crear nueva carpeta de versiÃƒÂ³n
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

                AgregarLog($"Ã¢Å“â€¦ PublicaciÃƒÂ³n v{nextV} completada satisfactoriamente.");
                MessageBox.Show($"La versiÃƒÂ³n {nextV} ha sido desplegada en el enjambre.", "Ãƒâ€°xito GalÃƒÂ¡ctico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { AgregarLog($"Ã¢ÂÅ’ Error fatal en publicaciÃƒÂ³n: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }

        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
        //  HELPERS
        // Ã¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢ÂÃ¢â€¢Â
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
                AgregarLog($"Ã¢Å¡Â  RobustDelete (Fase 1): {ex.Message}. Intentando desintegraciÃƒÂ³n forzada...");
                try
                {
                    // Fallback agresivo para locks de Windows (Memoria de Usuario)
                    var psi = new ProcessStartInfo("cmd.exe", $"/c rd /s /q \"{path}\"") { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(psi)?.WaitForExit();
                    if (Directory.Exists(path)) AgregarLog("Ã¢ÂÅ’ Error: La carpeta resiste la eliminaciÃƒÂ³n forzada.");
                }
                catch (Exception ex2) { AgregarLog($"Ã¢Å¡Â  RobustDelete (Fase 2): {ex2.Message}"); }
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
                catch (Exception ex) { AgregarLog($"Ã¢Å¡Â  RunCommand({cmd}): {ex.Message}"); return -1; }
            });
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string f in Directory.GetFiles(source)) try { File.Copy(f, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(f)), true); } catch { }
            foreach (string d in Directory.GetDirectories(source)) CopyDirectory(d, System.IO.Path.Combine(dest, System.IO.Path.GetFileName(d)));
        }
        // Ã¢â€â‚¬Ã¢â€â‚¬ Performance & HW Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        private void ActualizarMonitores()
        {
            try {
                // Launcher RAM
                long mem = Process.GetCurrentProcess().PrivateMemorySize64 / 1024 / 1024;
                RamPerfText.Text = $"{mem} MB";

                CpuPerfText.Text = _gameProcess != null && !_gameProcess.HasExited ? "Juego activo" : "Idle";
            } catch { }
        }

        private void AplicarPrioridad()
        {
            if (_gameProcess == null || _gameProcess.HasExited) return;
            try {
                _gameProcess.PriorityClass = ProcessPriorityClass.High;
                AgregarLog("Ã°Å¸Å¡â‚¬ Prioridad de proceso establecida en ALTA.");
            } catch { }
        }

        // Ã¢â€â‚¬Ã¢â€â‚¬ UI Helpers Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬Ã¢â€â‚¬
        private void AnimateView(FrameworkElement element)
        {
            element.Visibility = Visibility.Visible;
            var sb = (Storyboard)FindResource("TabChangeEffect");
            sb.Begin(element);
        }

        public void CambiarVista(string vista) => NavigationService.Instance.NavigateTo(vista, this);

        #region DISCOVERY / TUTORIAL SYSTEM
        private int _tutorialStep = 0;
        private readonly List<(string target, string title, string content, Point pos)> _tutorialSteps = new()
        {
            ("NavHome", "Comandante: Centro de Mando", "Aqu\u00ED es donde ocurre la acci\u00F3n. Elige tu perfil de juego y l\u00E1nzate al abismo con un solo clic.", new Point(250, 150)),
            ("NavSistemas", "Sistemas Pepa", "Ajusta la RAM, selecciona el Java adecuado (\u00A1Ahora autom\u00E1tico!) y revisa la consola oficial de Pepita.", new Point(250, 200)),
            ("PlayButton", "Secuencia de Inicio", "El motor Kraken est\u00E1 optimizado. Pulsa este bot\u00F3n para entrar al servidor con todos los mods sincronizados.", new Point(480, 500)),
            ("UpdateBadge", "N\u00FAcleo Siempre Vivo", "Si ves este mensaje parpadeando, hay una nueva versi\u00F3n del motor disponible. \u00A1Dale clic!", new Point(10, 560))
        };

        private void StartDiscovery()
        {
            _tutorialStep = 0;
            TutorialOverlay.Visibility = Visibility.Visible;
            ShowTutorialStep();
        }

        private void ShowTutorialStep()
        {
            if (_tutorialStep < 0 || _tutorialStep >= _tutorialSteps.Count)
            {
                EndDiscovery();
                return;
            }

            var step = _tutorialSteps[_tutorialStep];
            TutorialTitle.Text = step.title;
            TutorialContent.Text = step.content;

            // Positioning of the card
            TutorialCard.HorizontalAlignment = HorizontalAlignment.Left;
            TutorialCard.VerticalAlignment = VerticalAlignment.Top;
            TutorialCard.Margin = new Thickness(step.pos.X, step.pos.Y, 0, 0);

            HighlightElement(step.target);
        }

        private void HighlightElement(string elementName)
        {
            try
            {
                var element = FindName(elementName) as FrameworkElement;
                if (element == null) return;

                var transform = element.TransformToVisual(this);
                Point pos = transform.Transform(new Point(0, 0));

                TutorialFocusRect.Rect = new Rect(pos.X - 5, pos.Y - 5, element.ActualWidth + 10, element.ActualHeight + 10);
            }
            catch { }
        }

        private void BtnNextTutorial_Click(object sender, RoutedEventArgs e)
        {
            _tutorialStep++;
            if (_tutorialStep >= _tutorialSteps.Count)
                EndDiscovery();
            else
                ShowTutorialStep();
        }

        private void BtnSkipTutorial_Click(object sender, RoutedEventArgs e)
        {
            EndDiscovery();
        }

        private void EndDiscovery()
        {
            TutorialOverlay.Visibility = Visibility.Collapsed;
            _session.HasFinishedDiscovery = true;
            GuardarSesion();
            AgregarLog("âœ¨ Fase de descubrimiento completada. \u00A1Bienvenido, Comandante!");
        }
        #endregion


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
                AgregarLog("Ã°Å¸Â§Â¹ CachÃƒÂ© y archivos temporales eliminados con ÃƒÂ©xito.");
                MessageBox.Show("CachÃƒÂ© y archivos temporales limpiados.", "Limpieza completada", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) { AgregarLog($"Ã¢ÂÅ’ Error limpiando cachÃƒÂ©: {ex.Message}"); }
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
