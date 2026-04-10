using System;
using System.Windows.Controls;
using NebulaLauncher.Modules;

namespace NebulaLauncher.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private Panel? _container;

        public void Initialize(Panel container)
        {
            _container = container;
        }

        public void Show(string message, string title = "TRANSMISIÓN", string icon = "📡")
        {
            if (_container == null) return;
            
            var toast = new NotificationToast(message, title, icon);
            toast.OnClosed += () =>
            {
                _container.Dispatcher.Invoke(() => _container.Children.Remove(toast));
            };
            
            _container.Children.Add(toast);
        }

        public void ShowSuccess(string message) => Show(message, "ÉXITO", "✔️");
        public void ShowError(string message) => Show(message, "ALERTA", "⚠️");
        public void ShowInfo(string message) => Show(message, "INFO", "ℹ️");
    }
}
