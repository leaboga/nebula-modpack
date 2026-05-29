using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace KrakenLauncher.Modules
{
    public partial class BlueMapView : UserControl
    {
        private readonly string _mapUrl;
        private static readonly string WebViewData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KrakenLauncher", "webview_data");
        private readonly string _mapId;

        public BlueMapView(string serverIp, string port, string mapId)
        {
            InitializeComponent();
            _mapId = string.IsNullOrWhiteSpace(mapId) ? "world" : mapId;
            string p = string.IsNullOrWhiteSpace(port) ? "8100" : port;
            // Precise format provided by user: #id:x:y:z:zoom:pitch:yaw:distance:perspective
            _mapUrl = $"http://{serverIp}:{p}/#{_mapId}:688:0:-48:1500:0:0:0:0:perspective";
            _ = InitWebView();
        }

        private async System.Threading.Tasks.Task InitWebView()
        {
            try
            {
                // Set dedicated user data folder to avoid permission issues
                Directory.CreateDirectory(WebViewData);
                var env = await CoreWebView2Environment.CreateAsync(null, WebViewData);
                await MapView.EnsureCoreWebView2Async(env);
                
                MapView.CoreWebView2.Settings.AreDevToolsEnabled   = false;
                MapView.CoreWebView2.Settings.IsStatusBarEnabled    = false;
                MapView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
                
                MapView.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    Dispatcher.Invoke(() => {
                        if (e.IsSuccess)
                        {
                            LoadingLabel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            LoadingLabel.Text = $"⚠ Error en BlueMap: {e.WebErrorStatus}\nURL: {_mapUrl}";
                            LoadingLabel.Visibility = Visibility.Visible;
                        }
                    });
                };

                MapView.Source = new Uri(_mapUrl);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    LoadingLabel.Text = $"⚠ Error WebView2: {ex.Message}\nURLintentada: {_mapUrl}");
            }
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try { LoadingLabel.Text = "🗺️  Cargando BlueMap..."; LoadingLabel.Visibility = Visibility.Visible; MapView.CoreWebView2?.Reload(); }
            catch { }
        }

        private void OpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_mapUrl) { UseShellExecute = true }); }
            catch { }
        }

        public void Stop()
        {
            try
            {
                MapView?.Dispose();
            }
            catch { }
        }
    }
}
