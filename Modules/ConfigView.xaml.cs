using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NebulaLauncher.Modules
{
    public partial class ConfigView : UserControl
    {
        private readonly ConfigManager _configManager;
        private readonly MainWindow _mainWindow;
        private bool _initializing = true;

        public ConfigView(MainWindow mainWindow)
        {
            _mainWindow      = mainWindow;
            InitializeComponent();
            _initializing    = true;
            _configManager = new ConfigManager(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "NebulaLauncher", "minecraft"));

            ServerIpBox.Text = _mainWindow.Session.ServerIp;
            BlueMapPortBox.Text = _mainWindow.Session.BlueMapPort;
            MapIdBox.Text = _mainWindow.Session.BlueMapId;
            WebhookBox.Text = _mainWindow.Session.CrashWebhookUrl;
            BgPathBox.Text = _mainWindow.Session.BackgroundImagePath;
            
            if (_mainWindow.CurrentProfile != null)
            {
                RamSlider.Value = _mainWindow.CurrentProfile.RamGB;
                RamValueText.Text = _mainWindow.CurrentProfile.RamGB.ToString();
                JavaPathBox.Text = _mainWindow.CurrentProfile.JavaPath;
            }
            
            InstanceNameBox.Text = _mainWindow.CurrentProfile?.Name ?? "default";
            _initializing    = false;
            UpdatePreview(_mainWindow.Session.BackgroundImagePath);
        }

        private void UpdatePreview(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                BgPreviewImage.Source = null;
                PreviewPlaceholderText.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.DecodePixelHeight = 80; // Optimize for preview size
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                BgPreviewImage.Source = bitmap;
                PreviewPlaceholderText.Visibility = Visibility.Collapsed;
            }
            catch 
            {
                BgPreviewImage.Source = null;
                PreviewPlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void BtnBrowseBackground_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos de Imagen|*.jpg;*.jpeg;*.png;*.webp;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                _mainWindow.Session.BackgroundImagePath = dlg.FileName;
                _mainWindow.GuardarSesion();
                BgPathBox.Text = dlg.FileName;
                _mainWindow.ActualizarFondo();
                UpdatePreview(dlg.FileName);
            }
        }

        private void BtnResetBackground_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Session.BackgroundImagePath = "";
            _mainWindow.GuardarSesion();
            BgPathBox.Text = "";
            _mainWindow.ActualizarFondo();
            UpdatePreview("");
        }

        private void ServerIpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null) return;
            string ip = ServerIpBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) return;
            _mainWindow.Session.ServerIp = ip;
            _mainWindow.GuardarSesion();
            _ = _mainWindow.ForceUpdateStatus();
        }

        private void Theme_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border btn && btn.Tag is string hex)
            {
                _mainWindow.Session.AccentColor = hex;
                _mainWindow.GuardarSesion();
                _mainWindow.ActualizarColorTema();
                
                // Actualizar bordes de selección (UI simple feedback)
                ThemeNebula.BorderThickness  = new Thickness(hex == "#7C3AED" ? 2 : 1);
                ThemeCrimson.BorderThickness = new Thickness(hex == "#EF4444" ? 2 : 1);
                ThemeEmerald.BorderThickness = new Thickness(hex == "#10B981" ? 2 : 1);
            }
        }

        private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_initializing || _mainWindow?.Session == null || _mainWindow.CurrentProfile == null) return;
            int val = (int)RamSlider.Value;
            RamValueText.Text = val.ToString();
            _mainWindow.CurrentProfile.RamGB = val;
            _mainWindow.GuardarSesion();
        }

        private void BtnAutoOptimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener RAM total del sistema en GB
                var gc       = GC.GetGCMemoryInfo();
                double total = (double)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024 * 1024);
                
                // Sugerir el 50% de la RAM total, máximo 12GB para MC
                int suggested = (int)Math.Min(12, Math.Max(2, total / 2));
                
                if (_mainWindow.CurrentProfile != null)
                {
                    _mainWindow.CurrentProfile.RamGB = suggested;
                    RamSlider.Value = suggested;
                }
                MessageBox.Show($"Nebula sugiere {suggested}GB para tu sistema ({Math.Round(total,1)}GB Detectados).", "Optimizaci\u00F3n Lista", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { MessageBox.Show("No se pudo detectar la memoria autom\u00E1ticamente.", "Error"); }
        }

        private void BtnOpenShaders_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("shaderpacks");
        private void BtnOpenPacks_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("resourcepacks");
        private void BtnOpenConfigs_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("config");
        private void BtnOpenGame_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("");

        private void BtnBrowseJava_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Java Executable|java.exe" };
            if (dlg.ShowDialog() == true && _mainWindow.CurrentProfile != null)
            {
                _mainWindow.CurrentProfile.JavaPath = dlg.FileName;
                _mainWindow.GuardarSesion();
                JavaPathBox.Text = dlg.FileName;
            }
        }

        private async void BtnDownloadShaders_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "⏳ Descargando...";
            try
            {
                string shaderDir = Path.Combine(_mainWindow.GameFolder, "shaderpacks");
                Directory.CreateDirectory(shaderDir);
                string zipPath = Path.Combine(shaderDir, "Nebula-Shaders.zip");
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    var data = await client.GetByteArrayAsync("https://github.com/leaboga/nebula-modpack/releases/download/assets/shaders.zip");
                    await File.WriteAllBytesAsync(zipPath, data);
                }
                
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, shaderDir, true);
                File.Delete(zipPath);
                
                MessageBox.Show("✅ 'Nebula Shaders' instalados correctamente.", "Descarga Completada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"Error al descargar shaders: {ex.Message}", "Error"); }
            finally { btn.IsEnabled = true; btn.Content = "📦 Descargar Shaders"; }
        }

        private void OpenGameSubfolder(string sub)
        {
            try 
            { 
                string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher", "minecraft", sub);
                Directory.CreateDirectory(appFolder);
                Process.Start("explorer.exe", appFolder);
            }
            catch { }
        }

        private void BlueMapPortBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null) return;
            _mainWindow.Session.BlueMapPort = BlueMapPortBox.Text.Trim();
            _mainWindow.GuardarSesion();
        }

        private void MapIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null) return;
            _mainWindow.Session.BlueMapId = MapIdBox.Text.Trim();
            _mainWindow.GuardarSesion();
        }

        private void WebhookBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null) return;
            _mainWindow.Session.CrashWebhookUrl = WebhookBox.Text.Trim();
            _mainWindow.GuardarSesion();
        }

        private void Color_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border btn && btn.Tag is string hex)
            {
                _mainWindow.Session.AccentColor = hex;
                _mainWindow.GuardarSesion();
                _mainWindow.ActualizarColorTema();
            }
        }

        private async void PapaModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn       = (Button)sender;
            btn.IsEnabled = false;
            try
            {
                await _configManager.ApplyPerformancePreset("Papa");
                MessageBox.Show("✅ Modo Papa aplicado.\nGráficos optimizados para máximo rendimiento.",
                                "Preset aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error aplicando preset:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { btn.IsEnabled = true; }
        }

        private async void UltraModeBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn       = (Button)sender;
            btn.IsEnabled = false;
            try
            {
                await _configManager.ApplyPerformancePreset("Ultra");
                MessageBox.Show("✅ Modo Ultra aplicado.\nGráficos en calidad máxima con shaders.",
                                "Preset aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error aplicando preset:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { btn.IsEnabled = true; }
        }

        private async void BtnRepairPack_Click(object sender, RoutedEventArgs e)
        {
            await _mainWindow.SincronizarTodoAsync();
        }

        private void BtnChangeInstance_Click(object sender, RoutedEventArgs e)
        {
            // La lógica de instancias ahora se maneja por perfiles en el Home
            MessageBox.Show("Las instancias ahora se gestionan desde la pantalla de Inicio mediante Perfiles.", "Informaci\u00F3n", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnViewCrashes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string crashDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                                "NebulaLauncher", "minecraft", "crash-reports");
                Directory.CreateDirectory(crashDir);
                Process.Start("explorer.exe", crashDir);
            }
            catch { }
        }
    }
}
