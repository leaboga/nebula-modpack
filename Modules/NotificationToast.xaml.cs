using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace KrakenLauncher.Modules
{
    public partial class NotificationToast : UserControl
    {
        public event Action? OnClosed;

        public NotificationToast(string message, string title = "TRANSMISIÓN", string icon = "📡")
        {
            InitializeComponent();
            MessageText.Text = message;
            TitleText.Text = title;
            IconLabel.Text = icon;
            
            Loaded += async (s, e) =>
            {
                var sb = (Storyboard)Resources["EnterAnim"];
                sb.Begin(this);
                await Task.Delay(5000);
                Close();
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private async void Close()
        {
            var sb = (Storyboard)Resources["ExitAnim"];
            sb.Begin(this);
            await Task.Delay(400);
            OnClosed?.Invoke();
        }
    }
}
