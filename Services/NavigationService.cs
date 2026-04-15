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
                case "changelog":
                    UpdateHeaders("NOTIFICACIONES", "Bitácora de Versiones");
                    SwitchToModule(new ChangelogView(), main);
                    break;
                case "configsync":
                    UpdateHeaders("SINCRONIZACIÓN", "Setup de Pepa");
                    // We reuse the ConfigView logic but it could be a separate view.
                    // For now, as requested, we make the feature visible.
                    SwitchToModule(new ConfigView(main), main);
                    break;
                case "settings":
                    UpdateHeaders("SISTEMAS", "Configuración del Iniciador");
                    SwitchToModule(new ConfigView(main), main);
                    break;
                case "social":
                    UpdateHeaders("RED EXTERNA", "Comunidad KRAKEN");
                    SwitchToModule(new SocialView(main.Session.ServerIp, main.Session.Username), main);
                    break;
                case "perf":
                    UpdateHeaders("OPTIMIZACIÓN", "Rendimiento y Memoria");
                    SwitchToModule(new PerformanceView(main), main);
                    break;
                case "screenshots":
                    UpdateHeaders("ARCHIVOS", "Capturas de Despliegue");
                    SwitchToModule(new ScreenshotsView(main.GameFolder), main);
                    break;
                case "modmanager":
                    UpdateHeaders("LOGÍSTICA", "Gestión de Módulos (Local)");
                    var mv = new ModManagerView(main.GameFolder, main.CurrentProfile);
                    mv.OnSyncRequested += main.SincronizarTodoAsync;
                    SwitchToModule(mv, main);
                    break;
                case "modhub":
                    UpdateHeaders("CENTRO DE RECURSOS", "Biblioteca de Mods");
                    SwitchToModule(new VaultView(main.GameFolder, main.CurrentProfile), main);
                    break;
                case "crash":
                    UpdateHeaders("DIAGNÓSTICO", "Herramientas de Soporte");
                    SwitchToModule(new CrashDiagnosticView(main.GetCrashReporter()), main);
                    break;
                case "map":
                    UpdateHeaders("INTELIGENCIA", "Servicio de Cartografía");
                    SwitchToModule(new BlueMapView(main.Session.ServerIp, main.Session.BlueMapPort, main.Session.BlueMapId), main);
                    break;
                case "hosting":
                    UpdateHeaders("INFRAESTRUCTURA", "Servicios Hosting (BETA)");
                    SwitchToModule(new HostingServiceView(), main);
                    break;
                case "localhost":
                    UpdateHeaders("NODOS LOCALES", "Servidor de Pruebas");
                    SwitchToModule(new ServerHostView(), main);
                    break;
                case "modpacks":
                    UpdateHeaders("MODPACK HUB", "Catálogo de Expediciones");
                    SwitchToModule(new ModpackView(), main);
                    break;
                case "console":
                    UpdateHeaders("TERMINAL", "Consola de Sistema");
                    SwitchToModule(new ConsoleView(), main);
                    break;
            }
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
