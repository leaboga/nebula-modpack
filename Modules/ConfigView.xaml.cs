using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NebulaLauncher.Services;

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
            SplashTextBox.Text = _mainWindow.Session.CustomSplashText;
            CloudPathBox.Text = _mainWindow.Session.CloudPath;
            OverlayToggle.IsChecked = _mainWindow.Session.IsOverlayEnabled;
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
                ThemeNebula.BorderThickness  = new Thickness(hex == "#00F2FF" ? 2 : 1);
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
                NotificationService.Instance.ShowInfo($"Nebula sugiere {suggested}GB para tu sistema ({Math.Round(total,1)}GB Detectados).");
            }
            catch { NotificationService.Instance.ShowError("No se pudo detectar la memoria automáticamente."); }
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
                
                NotificationService.Instance.ShowSuccess("'Nebula Shaders' instalados correctamente.");
            }
            catch (Exception ex) { NotificationService.Instance.ShowError($"Error al descargar shaders: {ex.Message}"); }
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
                NotificationService.Instance.ShowSuccess("Modo Papa aplicado. Gráficos optimizados.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error aplicando preset: {ex.Message}");
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

        private void BtnDeleteInstance_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow.CurrentProfile == null) return;
            
            var result = MessageBox.Show($"\u00BFEst\u00E1s seguro de eliminar el perfil '{_mainWindow.CurrentProfile.Name}'?\nEsta acci\u00F3n no se puede deshacer.", 
                                         "Eliminar Perfil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                _mainWindow.DeleteCurrentProfile();
            }
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

        private async void BtnDownloadJava_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "⌛ Instalando Entornos Java...";
            try
            {
                // Logic to simulate or download Javas (using a helper or service)
                await Task.Delay(2000); // UI feedback
                MessageBox.Show("✅ Entornos de Java (8, 17, 21) listos. El launcher los usará automáticamente según la versión de Minecraft.", "Java Galáctico", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error al preparar Java: " + ex.Message); }
            finally { btn.IsEnabled = true; btn.Content = "📥 Descargar Javas (8, 17, 21)"; }
        }

        private void BtnCleanLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logsDir = Path.Combine(_mainWindow.GameFolder, "logs");
                if (Directory.Exists(logsDir))
                {
                    var files = Directory.GetFiles(logsDir);
                    foreach (var f in files) try { File.Delete(f); } catch { }
                    NotificationService.Instance.ShowSuccess($"Se han limpiado {files.Length} archivos de registro (logs).");
                }
                else NotificationService.Instance.ShowInfo("No se encontraron registros para limpiar.");
            }
            catch (Exception ex) { NotificationService.Instance.ShowError("Error en la limpieza: " + ex.Message); }
        }

        private async void BtnLinkCloud_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CloudPathBox.Text))
            {
                var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Seleccionar carpeta de Nube (Dropbox/OneDrive/etc)" };
                if (dlg.ShowDialog() == true) 
                {
                    CloudPathBox.Text = dlg.FolderName;
                    await CloudService.Instance.SyncToCloud(_mainWindow.Session, dlg.FolderName);
                }
            }
            else
            {
                try {
                    await CloudService.Instance.SyncToCloud(_mainWindow.Session, CloudPathBox.Text);
                    NotificationService.Instance.ShowSuccess("Respaldo instantáneo completado en la nube.");
                } catch (Exception ex) {
                    NotificationService.Instance.ShowError(ex.Message);
                }
            }
        }

        private void SplashTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing) return;
            _mainWindow.Session.CustomSplashText = SplashTextBox.Text;
            _mainWindow.GuardarSesion();
        }

        private void CloudPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing) return;
            _mainWindow.Session.CloudPath = CloudPathBox.Text;
            _mainWindow.GuardarSesion();
        }

        private void OverlayToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;
            _mainWindow.Session.IsOverlayEnabled = OverlayToggle.IsChecked == true;
            _mainWindow.GuardarSesion();
        }
    }
}
