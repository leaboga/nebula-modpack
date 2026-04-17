using System;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace KrakenLauncher
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        public static TaskbarIcon? TrayIcon { get; private set; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Evita que Windows agrupe el icono con versiones viejas o use el icono de Nebula de la caché
            SetCurrentProcessExplicitAppUserModelID("KRAKEN.Engine.v4");

            const string appName = "Global\\KrakenLauncher-SingleInstance-Check";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // Otra instancia ya está en ejecución
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        IntPtr handle = process.MainWindowHandle;
                        if (handle != IntPtr.Zero)
                        {
                            ShowWindow(handle, SW_RESTORE);
                            SetForegroundWindow(handle);
                        }
                        break;
                    }
                }
                
                Shutdown();
                return;
            }

            // Global Exception Handler
            AppDomain.CurrentDomain.UnhandledException += (s, ev) => HandleGlobalError(ev.ExceptionObject as Exception);
            DispatcherUnhandledException += (s, ev) => { HandleGlobalError(ev.Exception); ev.Handled = true; };
            
            base.OnStartup(e);
            try { TrayIcon = (TaskbarIcon)FindResource("TrayIcon"); } catch { }
        }

        private void HandleGlobalError(Exception? ex)
        {
            if (ex == null) return;
            string msg = $"🌌 Error Galáctico Detectado:\n\n{ex.Message}\n\nTipo: {ex.GetType().Name}\nDetalles:\n{ex.StackTrace}";
            MessageBox.Show(msg, "KRAKEN Diagnostics", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                MainWindow.Activate();
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
            }
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow is MainWindow w)
                w.CerrarDefinitivo();
            else
                Shutdown();
        }
    }
}
