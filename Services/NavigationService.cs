using System;
using System.Collections.Generic;
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

        private readonly Dictionary<string, HubView> _hubCache = new();
        private ContentControl? _container;
        private FrameworkElement? _homeView;
        private TextBlock? _viewLabel;
        private TextBlock? _viewTitle;
        private Action? _onStopCurrent;
        private string _lastGameFolder = "";

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
            ResetCacheIfProfileChanged(main);

            _container.Visibility = Visibility.Collapsed;
            _homeView.Visibility = Visibility.Collapsed;

            switch (vista)
            {
                case "home":
                    UpdateHeaders("INICIO", "Panel principal");
                    _homeView.Visibility = Visibility.Visible;
                    AnimateView(_homeView, main);
                    break;

                case "sistemas":
                case "settings":
                case "configsync":
                case "perf":
                case "console":
                case "crash":
                    UpdateHeaders("SISTEMA", "Configuracion y diagnostico");
                    SwitchToModule(GetOrCreateHub("systems", main, () => new List<HubView.HubTab>
                    {
                        new() { Label = "Motor", Icon = "CORE", HeaderLabel = "SINCRONIZACION", HeaderTitle = "Setup de Kraken", ViewFactory = () => new ConfigView(main) },
                        new() { Label = "Rendimiento", Icon = "TPS", HeaderLabel = "OPTIMIZACION", HeaderTitle = "Rendimiento y RAM", ViewFactory = () => new PerformanceView(main) },
                        new() { Label = "Consola", Icon = "LOG", HeaderLabel = "TERMINAL", HeaderTitle = "Consola de Sistema", ViewFactory = () => new ConsoleView() },
                        new() { Label = "Diagnostico", Icon = "FIX", HeaderLabel = "DIAGNOSTICO", HeaderTitle = "Herramientas de Soporte", ViewFactory = () => new CrashDiagnosticView(main.GetCrashReporter()) }
                    }), main);
                    break;

                case "recursos":
                case "modhub":
                case "modmanager":
                case "modpacks":
                case "changelog":
                    UpdateHeaders("RECURSOS", "Mods y archivos locales");
                    SwitchToModule(GetOrCreateHub("resources", main, () => new List<HubView.HubTab>
                    {
                        new() { Label = "Biblioteca", Icon = "LIB", HeaderLabel = "CENTRO DE RECURSOS", HeaderTitle = "Modulos Externos", ViewFactory = () => new VaultView(main.GameFolder, main.CurrentProfile) },
                        new() { Label = "Mis Mods", Icon = "JAR", HeaderLabel = "LOGISTICA", HeaderTitle = "Gestion Local", ViewFactory = () => CreateModManager(main) },
                        new() { Label = "Expediciones", Icon = "MAP", HeaderLabel = "MODPACK HUB", HeaderTitle = "Catalogo de Viajes", ViewFactory = () => new ModpackView() },
                        new() { Label = "Novedades", Icon = "NEW", HeaderLabel = "NOTIFICACIONES", HeaderTitle = "Bitacora de Versiones", ViewFactory = () => new ChangelogView() }
                    }), main);
                    break;

                case "red":
                case "social":
                case "map":
                case "hosting":
                case "localhost":
                case "screenshots":
                    UpdateHeaders("SERVIDOR", "Red, mapa y servidor local");
                    SwitchToModule(GetOrCreateHub("network", main, () => new List<HubView.HubTab>
                    {
                        new() { Label = "Comunidad", Icon = "COM", HeaderLabel = "RED EXTERNA", HeaderTitle = "Comunidad KRAKEN", ViewFactory = () => new SocialView(main.Session.ServerIp, main.Session.Username) },
                        new() { Label = "Mapa", Icon = "GPS", HeaderLabel = "INTELIGENCIA", HeaderTitle = "Servicio Cartografico", ViewFactory = () => new BlueMapView(main.Session.ServerIp, main.Session.BlueMapPort, main.Session.BlueMapId) },
                        new() { Label = "Hosting", Icon = "NET", HeaderLabel = "SERVICIOS", HeaderTitle = "Hosting ", ViewFactory = () => new HostingServiceView() },
                        new() { Label = "Local", Icon = "DEV", HeaderLabel = "NODOS", HeaderTitle = "Servidor de Desarrollo", ViewFactory = () => new ServerHostView() },
                        new() { Label = "Capturas", Icon = "IMG", HeaderLabel = "ARCHIVOS", HeaderTitle = "Registros Visuales", ViewFactory = () => new ScreenshotsView(main.GameFolder) }
                    }), main);
                    break;
            }
        }

        private HubView GetOrCreateHub(string key, MainWindow main, Func<List<HubView.HubTab>> tabsFactory)
        {
            if (_hubCache.TryGetValue(key, out var cached)) return cached;

            var hub = new HubView(main, tabsFactory());
            hub.OnHeaderUpdateRequested += UpdateHeaders;
            _hubCache[key] = hub;
            return hub;
        }

        private void ResetCacheIfProfileChanged(MainWindow main)
        {
            if (_lastGameFolder == main.GameFolder) return;

            _lastGameFolder = main.GameFolder;
            foreach (var hub in _hubCache.Values) hub.StopActive();
            _hubCache.Clear();
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
            if (ReferenceEquals(_container.Content, module)) return;
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
