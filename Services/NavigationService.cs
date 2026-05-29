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

        private ContentControl? _container;
        private FrameworkElement? _homeView;
        private TextBlock? _viewLabel;
        private TextBlock? _viewTitle;
        private Action? _onStopCurrent;
        private HubView? _systemsHub;
        private HubView? _resourcesHub;
        private HubView? _networkHub;

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
                    _systemsHub ??= CreateSystemsHub(main);
                    UpdateHeaders("SISTEMAS", "Nucleo de Control");
                    SwitchToModule(_systemsHub, main);
                    break;

                case "recursos":
                case "modhub":
                case "modmanager":
                case "modpacks":
                case "changelog":
                    _resourcesHub ??= CreateResourcesHub(main);
                    UpdateHeaders("RECURSOS", "Almacenamiento y Datos");
                    SwitchToModule(_resourcesHub, main);
                    break;

                case "red":
                case "social":
                case "map":
                case "hosting":
                case "localhost":
                case "screenshots":
                    _networkHub ??= CreateNetworkHub(main);
                    UpdateHeaders("RED ABISAL", "Comunicaciones y Flota");
                    SwitchToModule(_networkHub, main);
                    break;
            }
        }

        public void InvalidateCache()
        {
            _systemsHub?.StopActiveModule();
            _resourcesHub?.StopActiveModule();
            _networkHub?.StopActiveModule();
            _systemsHub = null;
            _resourcesHub = null;
            _networkHub = null;
        }

        private HubView CreateSystemsHub(MainWindow main)
        {
            var systemsTabs = new List<HubView.HubTab>
            {
                new HubView.HubTab { Label = "Configs", HeaderLabel = "SINCRONIZACION", HeaderTitle = "Configs y Ajustes", View = new ConfigView(main) },
                new HubView.HubTab { Label = "Rendimiento", HeaderLabel = "OPTIMIZACION", HeaderTitle = "Rendimiento y RAM", View = new PerformanceView(main) },
                new HubView.HubTab { Label = "Consola", HeaderLabel = "TERMINAL", HeaderTitle = "Consola de Sistema", View = new ConsoleView() },
                new HubView.HubTab { Label = "Soporte", HeaderLabel = "DIAGNOSTICO", HeaderTitle = "Herramientas de Soporte", View = new CrashDiagnosticView(main.GetCrashReporter()) }
            };

            var hub = new HubView(main, systemsTabs);
            hub.OnHeaderUpdateRequested += UpdateHeaders;
            return hub;
        }

        private HubView CreateResourcesHub(MainWindow main)
        {
            var resourcesTabs = new List<HubView.HubTab>
            {
                new HubView.HubTab { Label = "Biblioteca", HeaderLabel = "CENTRO DE RECURSOS", HeaderTitle = "Modulos Externos", View = new VaultView(main.GameFolder, main.CurrentProfile) },
                new HubView.HubTab { Label = "Mis Mods", HeaderLabel = "LOGISTICA", HeaderTitle = "Gestion Local", View = CreateModManager(main) },
                new HubView.HubTab { Label = "Expediciones", HeaderLabel = "MODPACK HUB", HeaderTitle = "Catalogo de Viajes", View = new ModpackView() },
                new HubView.HubTab { Label = "Novedades", HeaderLabel = "NOTIFICACIONES", HeaderTitle = "Bitacora de Versiones", View = new ChangelogView() }
            };

            var hub = new HubView(main, resourcesTabs);
            hub.OnHeaderUpdateRequested += UpdateHeaders;
            return hub;
        }

        public static string GetActiveServerIp(MainWindow main)
        {
            try
            {
                if (main.CurrentProfile != null)
                {
                    string serversDatPath = System.IO.Path.Combine(main.GameFolder, "servers.dat");
                    if (System.IO.File.Exists(serversDatPath))
                    {
                        byte[] data = System.IO.File.ReadAllBytes(serversDatPath);
                        byte[] searchPattern = new byte[] { 0x08, 0x00, 0x02, 0x69, 0x70 };
                        for (int i = 0; i < data.Length - searchPattern.Length - 2; i++)
                        {
                            bool match = true;
                            for (int j = 0; j < searchPattern.Length; j++)
                            {
                                if (data[i + j] != searchPattern[j]) { match = false; break; }
                            }
                            if (match)
                            {
                                int ipLengthIdx = i + searchPattern.Length;
                                int ipLength = (data[ipLengthIdx] << 8) | data[ipLengthIdx + 1];
                                int ipStartIdx = ipLengthIdx + 2;
                                if (ipStartIdx + ipLength <= data.Length)
                                {
                                    string fullIp = System.Text.Encoding.UTF8.GetString(data, ipStartIdx, ipLength);
                                    return fullIp.Contains(":") ? fullIp.Split(':')[0] : fullIp;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return main.Session.ServerIp;
        }

        private HubView CreateNetworkHub(MainWindow main)
        {
            string activeIp = GetActiveServerIp(main);
            var networkTabs = new List<HubView.HubTab>
            {
                new HubView.HubTab { Label = "Comunidad", HeaderLabel = "RED EXTERNA", HeaderTitle = "Comunidad KRAKEN", View = new SocialView(activeIp, main.Session.Username) },
                new HubView.HubTab { Label = "Mapa Abisal", HeaderLabel = "INTELIGENCIA", HeaderTitle = "Servicio Cartografico", View = new BlueMapView(activeIp, main.Session.BlueMapPort, main.Session.BlueMapId) },
                new HubView.HubTab { Label = "Infraestructura", HeaderLabel = "SERVICIOS", HeaderTitle = "Hosting Galactico", View = new HostingServiceView() },
                new HubView.HubTab { Label = "Pruebas", HeaderLabel = "NODOS", HeaderTitle = "Servidor de Desarrollo", View = new ServerHostView() },
                new HubView.HubTab { Label = "Capturas", HeaderLabel = "ARCHIVOS", HeaderTitle = "Registros Visuales", View = new ScreenshotsView(main.GameFolder) }
            };

            var hub = new HubView(main, networkTabs);
            hub.OnHeaderUpdateRequested += UpdateHeaders;
            return hub;
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
