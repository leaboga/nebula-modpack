using System;
using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace NebulaLauncher
{
    public partial class App : Application
    {
        public static TaskbarIcon? TrayIcon { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
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
            MessageBox.Show(msg, "Nebula Diagnostics", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            TrayIcon?.Dispose();
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
