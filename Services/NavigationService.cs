using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using KrakenLauncher.Modules;

namespace KrakenLauncher.Services
{
    public class NavigationService
    {
        private static NavigationService? _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        private ContentControl? _container;
        private FrameworkElement? _homeView;
        private TextBlock? _viewLabel;
        private TextBlock? _viewTitle;
        private Action? _onStopCurrent;

        public void Initialize(ContentControl container, FrameworkElement homeView, TextBlock viewLabel, TextBlock viewTitle, Action onStopCurrent)
        {
            _container = container;
            _homeView = homeView;
            _viewLabel = viewLabel;
            _viewTitle = viewTitle;
            _onStopCurrent = onStopCurrent;
        }

        public void NavigateTo(string vista, MainWindow main)
        {
            if (_container == null || _homeView == null) return;

            _onStopCurrent?.Invoke();

            _container.Visibility = Visibility.Collapsed;
            _homeView.Visibility = Visibility.Collapsed;
            _container.Content = null;

            switch (vista)
            {
                case "home":
                    UpdateHeaders("CENTRO DE OPERACIONES", "Bienvenido, Comandante");
                    _homeView.Visibility = Visibility.Visible;
                    AnimateView(_homeView, main);
                    break;

                case "sistemas":
                case "settings":
                case "configsync":
                case "perf":
                case "console":
                case "crash":
                    var systemsTabs = new List<HubView.HubTab>
                    {
                        new HubView.HubTab { Label = "Motor y Sinc", HeaderLabel = "SINCRONIZACIÓN", HeaderTitle = "Setup de Pepa", View = new ConfigView(main) },
                        new HubView.HubTab { Label = "Rendimiento", HeaderLabel = "OPTIMIZACIÓN", HeaderTitle = "Rendimiento y RAM", View = new PerformanceView(main) },
                        new HubView.HubTab { Label = "Consola", HeaderLabel = "TERMINAL", HeaderTitle = "Consola de Sistema", View = new ConsoleView() },
                        new HubView.HubTab { Label = "Diagnóstico", HeaderLabel = "DIAGNÓSTICO", HeaderTitle = "Herramientas de Soporte", View = new CrashDiagnosticView(main.GetCrashReporter()) }
                    };
                    var systemsHub = new HubView(main, systemsTabs);
                    systemsHub.OnHeaderUpdateRequested += UpdateHeaders;
                    UpdateHeaders("SISTEMAS", "Núcleo de Control");
                    SwitchToModule(systemsHub, main);
                    break;

                case "recursos":
                case "modhub":
                case "modmanager":
                case "modpacks":
                case "changelog":
                    var resourcesTabs = new List<HubView.HubTab>
                    {
                        new HubView.HubTab { Label = "Biblioteca", HeaderLabel = "CENTRO DE RECURSOS", HeaderTitle = "Módulos Externos", View = new VaultView(main.GameFolder, main.CurrentProfile) },
                        new HubView.HubTab { Label = "Mis Mods", HeaderLabel = "LOGÍSTICA", HeaderTitle = "Gestión Local", View = CreateModManager(main) },
                        new HubView.HubTab { Label = "Expediciones", HeaderLabel = "MODPACK HUB", HeaderTitle = "Catálogo de Viajes", View = new ModpackView() },
                        new HubView.HubTab { Label = "Novedades", HeaderLabel = "NOTIFICACIONES", HeaderTitle = "Bitácora de Versiones", View = new ChangelogView() }
                    };
                    var resourcesHub = new HubView(main, resourcesTabs);
                    resourcesHub.OnHeaderUpdateRequested += UpdateHeaders;
                    UpdateHeaders("RECURSOS", "Almacenamiento y Datos");
                    SwitchToModule(resourcesHub, main);
                    break;

                case "red":
                case "social":
                case "map":
                case "hosting":
                case "localhost":
                case "screenshots":
                    var networkTabs = new List<HubView.HubTab>
                    {
                        new HubView.HubTab { Label = "Comunidad", HeaderLabel = "RED EXTERNA", HeaderTitle = "Comunidad KRAKEN", View = new SocialView(main.Session.ServerIp, main.Session.Username) },
                        new HubView.HubTab { Label = "Mapa Abisal", HeaderLabel = "INTELIGENCIA", HeaderTitle = "Servicio Cartográfico", View = new BlueMapView(main.Session.ServerIp, main.Session.BlueMapPort, main.Session.BlueMapId) },
                        new HubView.HubTab { Label = "Infraestructura", HeaderLabel = "SERVICIOS", HeaderTitle = "Hosting Galáctico", View = new HostingServiceView() },
                        new HubView.HubTab { Label = "Pruebas", HeaderLabel = "NODOS", HeaderTitle = "Servidor de Desarrollo", View = new ServerHostView() },
                        new HubView.HubTab { Label = "Capturas", HeaderLabel = "ARCHIVOS", HeaderTitle = "Registros Visuales", View = new ScreenshotsView(main.GameFolder) }
                    };
                    var networkHub = new HubView(main, networkTabs);
                    networkHub.OnHeaderUpdateRequested += UpdateHeaders;
                    UpdateHeaders("RED ABISAL", "Comunicaciones y Flota");
                    SwitchToModule(networkHub, main);
                    break;
            }
        }

        private ModManagerView CreateModManager(MainWindow main)
        {
            var mv = new ModManagerView(main.GameFolder, main.CurrentProfile);
            mv.OnSyncRequested += main.SincronizarTodoAsync;
            return mv;
        }

        private void UpdateHeaders(string label, string title)
        {
            if (_viewLabel != null) _viewLabel.Text = label;
            if (_viewTitle != null) _viewTitle.Text = title;
        }

        private void SwitchToModule(UserControl module, MainWindow main)
        {
            if (_container == null) return;
            _container.Visibility = Visibility.Visible;
            _container.Content = module;
            AnimateView(_container, main);
        }

        private void AnimateView(FrameworkElement element, MainWindow main)
        {
            element.Visibility = Visibility.Visible;
            var sb = (Storyboard)main.FindResource("TabChangeEffect");
            sb.Begin(element);
        }
    }
}
