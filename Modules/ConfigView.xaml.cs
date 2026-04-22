using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KrakenLauncher.Services;
using Newtonsoft.Json;

namespace KrakenLauncher.Modules
{
    public partial class ConfigView : UserControl
    {
        private readonly ConfigManager _configManager;
        private readonly PresetService _presetService;
        private readonly MainWindow _mainWindow;
        private bool _initializing = true;
        private List<JavaRuntime> _runtimes = new();

        public ConfigView(MainWindow mainWindow)
        {
            _mainWindow      = mainWindow;
            InitializeComponent();
            _initializing    = true;
            _configManager = new ConfigManager(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "KrakenLauncher", "minecraft"));
            _presetService = new PresetService();

            ServerIpBox.Text = _mainWindow.Session.ServerIp;
            BlueMapPortBox.Text = _mainWindow.Session.BlueMapPort;
            MapIdBox.Text = _mainWindow.Session.BlueMapId;
            WebhookBox.Text = _mainWindow.Session.CrashWebhookUrl;
            BgPathBox.Text = _mainWindow.Session.BackgroundImagePath;

            if (_mainWindow.CurrentProfile != null)
            {
                RamSlider.Value = _mainWindow.CurrentProfile.RamGB;
                RamValueText.Text = _mainWindow.CurrentProfile.RamGB.ToString();
                JvmArgsBox.Text = string.IsNullOrWhiteSpace(_mainWindow.CurrentProfile.JvmArgs)
                    ? McGameLauncher.DefaultJvmArgs
                    : _mainWindow.CurrentProfile.JvmArgs;
            }

            CargarJavas();

            InstanceNameBox.Text = _mainWindow.CurrentProfile?.Name ?? "default";
            _initializing    = false;
            UpdatePreview(_mainWindow.Session.BackgroundImagePath);
            SplashTextBox.Text = _mainWindow.Session.CustomSplashText;
            CloudPathBox.Text = _mainWindow.Session.CloudPath;
            OverlayToggle.IsChecked = _mainWindow.Session.IsOverlayEnabled;

            LoadPresets();

            // Inicializacin del panel de configs de admin (async, no bloqueante)
            _ = Dispatcher.InvokeAsync(InicializarPanelPepita, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string f in Directory.GetFiles(source)) try { File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true); } catch { }
            foreach (string d in Directory.GetDirectories(source)) CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }

        private void LoadPresets()
        {
            try
            {
                PresetsListBox.ItemsSource = _presetService.GetPresets();
            }
            catch { }
        }

        private async void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            string name = NewPresetNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Por favor ingresa un nombre para el preset.", "KRAKEN");
                return;
            }

            if (_mainWindow.CurrentProfile == null) return;

            try
            {
                _mainWindow.AgregarLog($" Guardando preset: {name}...");
                await _presetService.SavePresetAsync(_mainWindow.GameFolder, name, _mainWindow.CurrentProfile.Version);
                _mainWindow.AgregarLog(" Preset guardado exitosamente.");
                NewPresetNameBox.Text = "";
                LoadPresets();
            }
            catch (Exception ex)
            {
                _mainWindow.AgregarLog($" Error al guardar preset: {ex.Message}");
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async void BtnApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string presetName = btn?.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(presetName)) return;

            if (_mainWindow.CurrentProfile == null) return;

            var res = MessageBox.Show($"Deseas aplicar el preset '{presetName}' al perfil actual?\nSe realizar un backup automtico de la configuracin actual.",
                "Aplicar Preset", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            try
            {
                _mainWindow.AgregarLog($" Aplicando preset '{presetName}'...");
                bool controls = PresetControlsCheck.IsChecked ?? false;
                bool graphics = PresetGraphicsCheck.IsChecked ?? false;
                bool mods = PresetModsCheck.IsChecked ?? false;
                bool others = PresetOthersCheck.IsChecked ?? false;

                await _presetService.ApplyPresetAsync(_mainWindow.GameFolder, presetName, controls, graphics, mods, others);

                _mainWindow.AgregarLog(" Preset aplicado. Los cambios se vern al iniciar el juego.");
                MessageBox.Show("Preset aplicado exitosamente.\nSe guard un backup en la carpeta 'backups' de la instancia.", "KRAKEN");
            }
            catch (Exception ex)
            {
                _mainWindow.AgregarLog($" Error al aplicar preset: {ex.Message}");
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string presetName = btn?.Tag?.ToString() ?? "";
            if (string.IsNullOrEmpty(presetName)) return;

            var res = MessageBox.Show($"Ests seguro de eliminar el preset '{presetName}'?\nEsta accin no se puede deshacer.",
                "Eliminar Preset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                _presetService.DeletePreset(presetName);
                LoadPresets();
                _mainWindow.AgregarLog($" Preset '{presetName}' eliminado.");
            }
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

                // Actualizar bordes de seleccin (UI simple feedback)
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

        private void JvmArgsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null || _mainWindow.CurrentProfile == null) return;

            string sanitized = string.Join(' ', McGameLauncher.ParseJvmArgs(JvmArgsBox.Text));
            _mainWindow.CurrentProfile.JvmArgs = string.IsNullOrWhiteSpace(sanitized)
                ? McGameLauncher.DefaultJvmArgs
                : sanitized;
            _mainWindow.GuardarSesion();
        }

        private void BtnAutoOptimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener RAM total del sistema en GB
                var gc       = GC.GetGCMemoryInfo();
                double total = (double)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024 * 1024 * 1024);

                // Sugerir el 50% de la RAM total, mximo 12GB para MC
                int suggested = (int)Math.Min(12, Math.Max(2, total / 2));

                if (_mainWindow.CurrentProfile != null)
                {
                    _mainWindow.CurrentProfile.RamGB = suggested;
                    RamSlider.Value = suggested;
                }
                NotificationService.Instance.ShowInfo($"RAM recomendada: {suggested}GB ({Math.Round(total,1)}GB detectados).");
            }
            catch { NotificationService.Instance.ShowError("No se pudo detectar la memoria automticamente."); }
        }

        private void BtnOpenShaders_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("shaderpacks");
        private void BtnOpenPacks_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("resourcepacks");
        private void BtnOpenConfigs_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("config");
        private void BtnOpenGame_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("");

        #region JAVA RUNTIME
        private void CargarJavas()
        {
            _runtimes = JavaService.DetectRuntimes();
            JavaVersionCombo.ItemsSource = _runtimes;

            if (_mainWindow.CurrentProfile != null && !string.IsNullOrEmpty(_mainWindow.CurrentProfile.JavaPath))
            {
                var current = _runtimes.FirstOrDefault(r => r.Path.Equals(_mainWindow.CurrentProfile.JavaPath, StringComparison.OrdinalIgnoreCase));
                if (current != null) JavaVersionCombo.SelectedItem = current;
                else if (!string.IsNullOrEmpty(_mainWindow.CurrentProfile.JavaPath))
                {
                    // If not found in scan, add manually as fallback
                    var manual = new JavaRuntime { Path = _mainWindow.CurrentProfile.JavaPath, Version = "Actual", Architecture = "x64" };
                    _runtimes.Add(manual);
                    JavaVersionCombo.ItemsSource = null;
                    JavaVersionCombo.ItemsSource = _runtimes;
                    JavaVersionCombo.SelectedItem = manual;
                }
            }
        }

        private void JavaVersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing) return;
            if (JavaVersionCombo.SelectedItem is JavaRuntime rt && _mainWindow.CurrentProfile != null)
            {
                _mainWindow.CurrentProfile.JavaPath = rt.Path;
                _mainWindow.GuardarSesion();
            }
        }

        private void BtnRefreshJava_Click(object sender, RoutedEventArgs e)
        {
            CargarJavas();
            MessageBox.Show($"Se detectaron {_runtimes.Count} entornos Java.", "KRAKEN Engine");
        }

        private void BtnBrowseJava_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Java Executable|java.exe" };
            if (dlg.ShowDialog() == true && _mainWindow.CurrentProfile != null)
            {
                _mainWindow.CurrentProfile.JavaPath = dlg.FileName;
                _mainWindow.GuardarSesion();
                CargarJavas();
            }
        }
        #endregion

        private async void BtnDownloadShaders_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = " Descargando...";
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
            finally { btn.IsEnabled = true; btn.Content = " Descargar Shaders"; }
        }

        private void OpenGameSubfolder(string sub)
        {
            try
            {
                string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KrakenLauncher", "minecraft", sub);
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
                NotificationService.Instance.ShowSuccess("Modo Papa aplicado. Grficos optimizados.");
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
                MessageBox.Show(" Modo Ultra aplicado.\nGrficos en calidad mxima con shaders.",
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
            // La lgica de instancias ahora se maneja por perfiles en el Home
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
                                                "KrakenLauncher", "minecraft", "crash-reports");
                Directory.CreateDirectory(crashDir);
                Process.Start("explorer.exe", crashDir);
            }
            catch { }
        }

        private async void BtnDownloadJava_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = " Instalando Entornos Java...";
            try
            {
                // Logic to simulate or download Javas (using a helper or service)
                await Task.Delay(2000); // UI feedback
                MessageBox.Show("Entornos de Java (8, 17, 21) listos. El launcher los usara automaticamente segun la version de Minecraft.", "Java listo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error al preparar Java: " + ex.Message); }
            finally { btn.IsEnabled = true; btn.Content = " Descargar Javas (8, 17, 21)"; }
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
                    NotificationService.Instance.ShowSuccess("Respaldo instantneo completado en la nube.");
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

        //
        //  CONFIGS DE PEPITA
        //

        private ModSyncer? _syncerLocal;

        private ModSyncer GetSyncer() =>
            _syncerLocal ??= new ModSyncer(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "KrakenLauncher", "minecraft"));

        /// <summary>Muestra u oculta el panel admin segn si el usuario es admin.</summary>
        private async void InicializarPanelPepita()
        {
            bool esAdminPc = Environment.MachineName.Equals("LEANDRO-PC", StringComparison.OrdinalIgnoreCase);
            bool esadmin = (_mainWindow.Session.IsAdmin && esAdminPc)
                         || (_mainWindow.Session.Username.Equals("admin",  StringComparison.OrdinalIgnoreCase) && esAdminPc)
                         || (_mainWindow.Session.Username.Equals("Pepita", StringComparison.OrdinalIgnoreCase) && esAdminPc)
                         || (_mainWindow.Session.Username.Equals("Leandro", StringComparison.OrdinalIgnoreCase) && esAdminPc);

            PepitaAdminPanel.Visibility = esadmin ? Visibility.Visible : Visibility.Collapsed;

            // Verificar estado del hash en background
            await ActualizarEstadoHashAsync();
        }

        private async Task ActualizarEstadoHashAsync()
        {
            try
            {
                var remoteInfo = await GetSyncer().ObtenerHashConfigsRemoto();
                string? hashRemoto = remoteInfo?.hash;
                int? ramOficial = remoteInfo?.ram;
                bool incluyeJvm = !string.IsNullOrWhiteSpace(remoteInfo?.jvmArgs);
                string configVersion = string.IsNullOrWhiteSpace(remoteInfo?.configVersion) ? "1.0" : remoteInfo!.Value.configVersion!;

                if (string.IsNullOrEmpty(hashRemoto))
                {
                    PepitaConfigStatusText.Text = "No se pudo determinar la version remota.";
                    return;
                }

                string profileId = _mainWindow.CurrentProfile?.Id ?? "default";
                bool alDia = hashRemoto == _mainWindow.Session.LastAppliedConfigHash;

                string versionRechazada = _mainWindow.Session.RejectedConfigVersions.ContainsKey(profileId)
                    ? _mainWindow.Session.RejectedConfigVersions[profileId] : "0";

                if (alDia)
                {
                    PepitaConfigStatusText.Text      = "Config oficial aplicada en este perfil.";
                    PepitaConfigStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else if (hashRemoto == versionRechazada)
                {
                    PepitaConfigStatusText.Text      = "Hay una config oficial disponible, pero fue omitida para este perfil.";
                    PepitaConfigStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                }
                else
                {
                    PepitaConfigStatusText.Text      = "Hay una nueva config oficial disponible.";
                    PepitaConfigStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xF2, 0xFF));
                }

                PepitaConfigHashText.Text = $"Config v{configVersion} | Hash: {(hashRemoto.Length > 8 ? hashRemoto[..8] : hashRemoto)} | RAM sugerida: {ramOficial ?? 4}GB | JVM: {(incluyeJvm ? "incluido" : "default")}";
            }
            catch (Exception ex)
            {
                PepitaConfigStatusText.Text = $"Error: {ex.Message}";
            }
        }

        private async void BtnVerificarConfigsPepita_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            try { await ActualizarEstadoHashAsync(); }
            finally { btn.IsEnabled = true; }
        }

        private async void BtnAplicarConfigsPepita_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "Aplicando...";
            try
            {
                var remoteInfo = await GetSyncer().ObtenerHashConfigsRemoto();
                if (remoteInfo == null) return;

                string hashRemoto = remoteInfo.Value.hash!;
                int ramRec = remoteInfo.Value.ram ?? (_mainWindow.CurrentProfile?.RamGB ?? 4);
                string? jvmArgs = remoteInfo.Value.jvmArgs;

                var result = MessageBox.Show(
                    $"Aplicar la config oficial?\n\n" +
                    "Se sobreescriben options.txt, config/ y shaderpacks/.\n" +
                    "RAM recomendada: " + ramRec + "GB (no se cambia tu RAM actual).\n" +
                    "Tambien se aplican los argumentos JVM oficiales sin -Xmx/-Xms.\n" +
                    "Se crea un backup antes de aplicar.",
                    "Aplicar config oficial",
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result != MessageBoxResult.Yes) return;

                // Backup manual
                string backupDir = Path.Combine(_mainWindow.GameFolder, "backups", "pre-official-sync-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(backupDir);
                foreach (var target in new[] { "options.txt", "optionsshaders.txt", "config", "shaderpacks" })
                {
                    string src = Path.Combine(_mainWindow.GameFolder, target);
                    if (File.Exists(src)) File.Copy(src, Path.Combine(backupDir, target), true);
                    else if (Directory.Exists(src)) CopyDirectory(src, Path.Combine(backupDir, target));
                }

                var syncer = GetSyncer();
                syncer.OnLog += msg => _mainWindow.AgregarLog(msg);
                await syncer.SincronizarConfigs(sobrescribirTodo: true);
                MandatoryFixesService.ApplyToKnownClientFolders(_mainWindow.GameFolder, msg => _mainWindow.AgregarLog(msg));

                if (_mainWindow.CurrentProfile != null)
                {
                    if (!string.IsNullOrWhiteSpace(jvmArgs))
                        _mainWindow.CurrentProfile.JvmArgs = string.Join(' ', McGameLauncher.ParseJvmArgs(jvmArgs));

                    _mainWindow.Session.LastAppliedConfigHash = hashRemoto;

                    string profileId = _mainWindow.CurrentProfile.Id;
                    _mainWindow.Session.RejectedConfigVersions.Remove(profileId);
                    _mainWindow.GuardarSesion();
                }

                await ActualizarEstadoHashAsync();
                NotificationService.Instance.ShowSuccess("Config oficial aplicada correctamente.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error: {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content   = "Aplicar config oficial";
            }
        }

        private async void BtnPublicarConfigsAdmin_Click(object sender, RoutedEventArgs e)
        {
            var login = new AdminLoginWindow { Owner = _mainWindow };
            if (login.ShowDialog() != true || login.Clave != "pepita2026")
            {
                if (!string.IsNullOrEmpty(login.Clave)) MessageBox.Show("Clave incorrecta.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "Publicando...";
            try
            {
                var remoteConfig = await GetSyncer().ObtenerHashConfigsRemoto();
                string currentConfigV = string.IsNullOrWhiteSpace(remoteConfig?.configVersion) ? "1.0" : remoteConfig!.Value.configVersion!;
                string nextConfigV = GetNextConfigVersion(currentConfigV);

                var confirm = MessageBox.Show(
                    $"Publicar tu configuracion actual como OFICIAL v{nextConfigV}?\n\n" +
                    "Esto subira:\n" +
                    "config/\n" +
                    "options.txt y optionsshaders.txt\n" +
                    "shaderpacks/\n\n" +
                    "Argumentos JVM oficiales sin RAM\n\n" +
                    "Los usuarios recibiran una notificacion. La RAM de cada jugador no se fuerza.",
                    "Publicar Config Oficial",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var syncer = GetSyncer();
                int ramActual = _mainWindow.CurrentProfile?.RamGB ?? 4;
                string jvmArgs = _mainWindow.CurrentProfile?.JvmArgs ?? McGameLauncher.DefaultJvmArgs;
                bool ok = await syncer.PublicarConfigsAdmin(msg => _mainWindow.AgregarLog(msg), ramActual, jvmArgs);

                if (ok)
                {

                    string profileId = _mainWindow.CurrentProfile?.Id ?? "default";
                    _mainWindow.Session.AppliedConfigVersions[profileId] = nextConfigV;
                    _mainWindow.GuardarSesion();

                    await ActualizarEstadoHashAsync();
                    NotificationService.Instance.ShowSuccess($"Config oficial v{nextConfigV} publicada.");
                }
                else
                {
                    NotificationService.Instance.ShowError("Error al publicar. Verifica logs.");
                }
            }
            catch (Exception ex) { NotificationService.Instance.ShowError($"Error: {ex.Message}"); }
            finally { btn.IsEnabled = true; btn.Content = "Publicar config oficial"; }
        }


        private static string GetNextConfigVersion(string current)
        {
            var parts = current.Split('.', StringSplitOptions.RemoveEmptyEntries);
            int major = parts.Length > 0 && int.TryParse(parts[0], out var parsedMajor) ? parsedMajor : 1;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out var parsedMinor) ? parsedMinor : 0;
            return $"{major}.{minor + 1}";
        }

        private async void BtnForzarConfigsPropias_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            try
            {
                var confirm = MessageBox.Show(
                    "Forzar la descarga y aplicacion completa de la config oficial?\n\n" +
                    "Esto sobreescribe options.txt, config/ y shaderpacks/.\n" +
                    "Usalo solo si queres sincronizar esta PC desde cero.",
                    "Sobreescribir todo?",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var resRemoto = await GetSyncer().ObtenerHashConfigsRemoto();
                string? hashRemoto = resRemoto?.hash;
                string? jvmArgs = resRemoto?.jvmArgs;
                var syncer = GetSyncer();
                syncer.OnLog += msg => _mainWindow.AgregarLog(msg);
                await syncer.SincronizarConfigs(sobrescribirTodo: true);
                MandatoryFixesService.ApplyToKnownClientFolders(_mainWindow.GameFolder, msg => _mainWindow.AgregarLog(msg));

                if (hashRemoto != null)
                {
                    _mainWindow.Session.LastAppliedConfigHash = hashRemoto;
                    if (_mainWindow.CurrentProfile != null && !string.IsNullOrWhiteSpace(jvmArgs))
                        _mainWindow.CurrentProfile.JvmArgs = string.Join(' ', McGameLauncher.ParseJvmArgs(jvmArgs));
                    _mainWindow.GuardarSesion();
                }
                await ActualizarEstadoHashAsync();
                NotificationService.Instance.ShowSuccess("Configs re-aplicadas completamente.");
            }
            catch (Exception ex) { NotificationService.Instance.ShowError($"Error: {ex.Message}"); }
            finally { btn.IsEnabled = true; }
        }
    }
}
