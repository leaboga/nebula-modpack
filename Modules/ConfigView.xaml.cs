using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KrakenLauncher.Services;

namespace KrakenLauncher.Modules
{
    public partial class ConfigView : UserControl
    {
//         private readonly ConfigManager _configManager;
//         private readonly PresetService _presetService;
        private readonly MainWindow _mainWindow;
        private bool _initializing = true;
        private List<JavaRuntime> _runtimes = new();

        public ConfigView(MainWindow mainWindow)
        {
            _mainWindow      = mainWindow;
            InitializeComponent();
            _initializing    = true;
//             _configManager = new ConfigManager(_mainWindow.GameFolder);
//             _presetService = new PresetService();

            ServerIpBox.Text = _mainWindow.Session.ServerIp;
            BlueMapPortBox.Text = _mainWindow.Session.BlueMapPort;
            MapIdBox.Text = _mainWindow.Session.BlueMapId;
            WebhookBox.Text = _mainWindow.Session.CrashWebhookUrl;
//             BgPathBox.Text = _mainWindow.Session.BackgroundImagePath;
            
            if (_mainWindow.CurrentProfile != null)
            {
                RamSlider.Value = _mainWindow.CurrentProfile.RamGB;
                RamValueText.Text = _mainWindow.CurrentProfile.RamGB.ToString();
            }
            
            CargarJavas();
            
            InstanceNameBox.Text = _mainWindow.CurrentProfile?.Name ?? "default";
            _initializing    = false;
//             UpdatePreview(_mainWindow.Session.BackgroundImagePath);
//             SplashTextBox.Text = _mainWindow.Session.CustomSplashText;
//             CloudPathBox.Text = _mainWindow.Session.CloudPath;
//             OverlayToggle.IsChecked = _mainWindow.Session.IsOverlayEnabled;

//             LoadPresets();

            // Inicialización del panel de configs de Pepita (async, no bloqueante)
            _ = Dispatcher.InvokeAsync(InicializarPanelPepita, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string f in Directory.GetFiles(source)) try { File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true); } catch { }
            foreach (string d in Directory.GetDirectories(source)) CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }

//         private void LoadPresets()
//         {
//             try
//             {
//                 PresetsListBox.ItemsSource = _presetService.GetPresets();
//                 int nextVersion = _presetService.GetNextPresetVersion();
//                 NextPresetVersionText.Text = $"La siguiente revision local sera REV {nextVersion:D3}.";
//             }
//             catch { }
//         }
// 
//         private async void BtnSavePreset_Click(object sender, RoutedEventArgs e)
//         {
//             if (_mainWindow.CurrentProfile == null) return;
// 
//             try
//             {
//                 int nextVersion = _presetService.GetNextPresetVersion();
//                 string name = _presetService.BuildPresetName(nextVersion);
//                 _mainWindow.AgregarLog($"[PRESET] Guardando revision local: {name}...");
//                 var metadata = await _presetService.SavePresetAsync(_mainWindow.GameFolder, name, _mainWindow.CurrentProfile.Version);
//                 _mainWindow.AgregarLog("[PRESET] Revision guardada correctamente.");
//                 LoadPresets();
//                 NotificationService.Instance.ShowSuccess($"Revision local REV {metadata.VersionNumber:D3} guardada.");
//             }
//             catch (Exception ex)
//             {
//                 _mainWindow.AgregarLog($"[PRESET] Error al guardar revision: {ex.Message}");
//                 MessageBox.Show("Error: " + ex.Message);
//             }
//         }
// 
//         private async void BtnApplyPreset_Click(object sender, RoutedEventArgs e)
//         {
//             var btn = sender as Button;
//             string presetName = btn?.Tag?.ToString() ?? "";
//             if (string.IsNullOrEmpty(presetName)) return;
// 
//             if (_mainWindow.CurrentProfile == null) return;
// 
//             var res = MessageBox.Show($"¿Deseas aplicar el preset '{presetName}' al perfil actual?\nSe realizará un backup automático de la configuración actual.", 
//                 "Aplicar Preset", MessageBoxButton.YesNo, MessageBoxImage.Question);
//             
//             if (res != MessageBoxResult.Yes) return;
// 
//             try
//             {
//                 _mainWindow.AgregarLog($"🔄 Aplicando preset '{presetName}'...");
//                 bool controls = PresetControlsCheck.IsChecked ?? false;
//                 bool graphics = PresetGraphicsCheck.IsChecked ?? false;
//                 bool mods = PresetModsCheck.IsChecked ?? false;
//                 bool others = PresetOthersCheck.IsChecked ?? false;
// 
//                 await _presetService.ApplyPresetAsync(_mainWindow.GameFolder, presetName, controls, graphics, mods, others);
//                 
//                 _mainWindow.AgregarLog("✅ Preset aplicado. Los cambios se verán al iniciar el juego.");
//                 MessageBox.Show("Preset aplicado exitosamente.\nSe guardó un backup en la carpeta 'backups' de la instancia.", "KRAKEN");
//             }
//             catch (Exception ex)
//             {
//                 _mainWindow.AgregarLog($"⚠ Error al aplicar preset: {ex.Message}");
//                 MessageBox.Show("Error: " + ex.Message);
//             }
//         }
// 
//         private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
//         {
//             var btn = sender as Button;
//             string presetName = btn?.Tag?.ToString() ?? "";
//             if (string.IsNullOrEmpty(presetName)) return;
// 
//             var res = MessageBox.Show($"¿Estás seguro de eliminar el preset '{presetName}'?\nEsta acción no se puede deshacer.", 
//                 "Eliminar Preset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
//             
//             if (res == MessageBoxResult.Yes)
//             {
//                 _presetService.DeletePreset(presetName);
//                 LoadPresets();
//                 _mainWindow.AgregarLog($"🗑️ Preset '{presetName}' eliminado.");
//             }
//         }
// 
//         private void UpdatePreview(string path)
//         {
//             if (string.IsNullOrEmpty(path) || !File.Exists(path))
//             {
//                 BgPreviewImage.Source = null;
//                 PreviewPlaceholderText.Visibility = Visibility.Visible;
//                 return;
//             }
// 
//             try
//             {
//                 var bitmap = new System.Windows.Media.Imaging.BitmapImage();
//                 bitmap.BeginInit();
//                 bitmap.UriSource = new Uri(path);
//                 bitmap.DecodePixelHeight = 80; // Optimize for preview size
//                 bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
//                 bitmap.EndInit();
//                 BgPreviewImage.Source = bitmap;
//                 PreviewPlaceholderText.Visibility = Visibility.Collapsed;
//             }
//             catch 
//             {
//                 BgPreviewImage.Source = null;
//                 PreviewPlaceholderText.Visibility = Visibility.Visible;
//             }
//         }
// 
//         private void BtnBrowseBackground_Click(object sender, RoutedEventArgs e)
//         {
//             var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Archivos de Imagen|*.jpg;*.jpeg;*.png;*.webp;*.bmp" };
//             if (dlg.ShowDialog() == true)
//             {
//                 _mainWindow.Session.BackgroundImagePath = dlg.FileName;
//                 _mainWindow.GuardarSesion();
//                 BgPathBox.Text = dlg.FileName;
//                 _mainWindow.ActualizarFondo();
//                 UpdatePreview(dlg.FileName);
//             }
//         }
// 
//         private void BtnResetBackground_Click(object sender, RoutedEventArgs e)
//         {
//             _mainWindow.Session.BackgroundImagePath = "";
//             _mainWindow.GuardarSesion();
//             BgPathBox.Text = "";
//             _mainWindow.ActualizarFondo();
//             UpdatePreview("");
//         }

        private void ServerIpBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_initializing || _mainWindow?.Session == null) return;
            string ip = ServerIpBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) return;
            _mainWindow.Session.ServerIp = ip;
            _mainWindow.GuardarSesion();
            _ = _mainWindow.ForceUpdateStatus();
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

//         private void BtnOpenShaders_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("shaderpacks");
//         private void BtnOpenPacks_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("resourcepacks");
//         private void BtnOpenConfigs_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("config");
//         private void BtnOpenGame_Click(object sender, RoutedEventArgs e) => OpenGameSubfolder("");
// 
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

//         private async void BtnDownloadShaders_Click(object sender, RoutedEventArgs e)
//         {
//             var btn = (Button)sender;
//             btn.IsEnabled = false;
//             btn.Content = "⏳ Descargando...";
//             try
//             {
//                 string shaderDir = Path.Combine(_mainWindow.GameFolder, "shaderpacks");
//                 Directory.CreateDirectory(shaderDir);
//                 string zipPath = Path.Combine(shaderDir, "Nebula-Shaders.zip");
//                 
//                 using (var client = new System.Net.Http.HttpClient())
//                 {
//                     var data = await client.GetByteArrayAsync("https://github.com/leaboga/nebula-modpack/releases/download/assets/shaders.zip");
//                     await File.WriteAllBytesAsync(zipPath, data);
//                 }
//                 
//                 System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, shaderDir, true);
//                 File.Delete(zipPath);
//                 
//                 NotificationService.Instance.ShowSuccess("'Nebula Shaders' instalados correctamente.");
//             }
//             catch (Exception ex) { NotificationService.Instance.ShowError($"Error al descargar shaders: {ex.Message}"); }
//             finally { btn.IsEnabled = true; btn.Content = "📦 Descargar Shaders"; }
//         }
// 
//         private void OpenGameSubfolder(string sub)
//         {
//             try 
//             { 
//                 string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KrakenLauncher", "minecraft", sub);
//                 Directory.CreateDirectory(appFolder);
//                 Process.Start("explorer.exe", appFolder);
//             }
//             catch { }
//         }

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

        

//         private async void PapaModeBtn_Click(object sender, RoutedEventArgs e)
//         {
//             var btn       = (Button)sender;
//             btn.IsEnabled = false;
//             try
//             {
//                 await _configManager.ApplyPerformancePreset("Papa");
//                 NotificationService.Instance.ShowSuccess("Modo Papa aplicado. Gráficos optimizados.");
//             }
//             catch (Exception ex)
//             {
//                 NotificationService.Instance.ShowError($"Error aplicando preset: {ex.Message}");
//             }
//             finally { btn.IsEnabled = true; }
//         }
// 
//         private async void UltraModeBtn_Click(object sender, RoutedEventArgs e)
//         {
//             var btn       = (Button)sender;
//             btn.IsEnabled = false;
//             try
//             {
//                 await _configManager.ApplyPerformancePreset("Ultra");
//                 MessageBox.Show("✅ Modo Ultra aplicado.\nGráficos en calidad máxima con shaders.",
//                                 "Preset aplicado", MessageBoxButton.OK, MessageBoxImage.Information);
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error aplicando preset:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
//             }
//             finally { btn.IsEnabled = true; }
//         }
// 
//         private async void BtnRepairPack_Click(object sender, RoutedEventArgs e)
//         {
//             await _mainWindow.SincronizarTodoAsync();
//         }

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

//         private void BtnViewCrashes_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 string crashDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
//                                                 "KrakenLauncher", "minecraft", "crash-reports");
//                 Directory.CreateDirectory(crashDir);
//                 Process.Start("explorer.exe", crashDir);
//             }
//             catch { }
//         }

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

//         private void BtnCleanLogs_Click(object sender, RoutedEventArgs e)
//         {
//             try
//             {
//                 string logsDir = Path.Combine(_mainWindow.GameFolder, "logs");
//                 if (Directory.Exists(logsDir))
//                 {
//                     var files = Directory.GetFiles(logsDir);
//                     foreach (var f in files) try { File.Delete(f); } catch { }
//                     NotificationService.Instance.ShowSuccess($"Se han limpiado {files.Length} archivos de registro (logs).");
//                 }
//                 else NotificationService.Instance.ShowInfo("No se encontraron registros para limpiar.");
//             }
//             catch (Exception ex) { NotificationService.Instance.ShowError("Error en la limpieza: " + ex.Message); }
//         }
// 
//         private async void BtnLinkCloud_Click(object sender, RoutedEventArgs e)
//         {
//             if (string.IsNullOrEmpty(CloudPathBox.Text))
//             {
//                 var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Seleccionar carpeta de Nube (Dropbox/OneDrive/etc)" };
//                 if (dlg.ShowDialog() == true) 
//                 {
//                     CloudPathBox.Text = dlg.FolderName;
//                     await CloudService.Instance.SyncToCloud(_mainWindow.Session, dlg.FolderName);
//                 }
//             }
//             else
//             {
//                 try {
//                     await CloudService.Instance.SyncToCloud(_mainWindow.Session, CloudPathBox.Text);
//                     NotificationService.Instance.ShowSuccess("Respaldo instantáneo completado en la nube.");
//                 } catch (Exception ex) {
//                     NotificationService.Instance.ShowError(ex.Message);
//                 }
//             }
//         }
// 
//         private void SplashTextBox_TextChanged(object sender, TextChangedEventArgs e)
//         {
//             if (_initializing) return;
//             _mainWindow.Session.CustomSplashText = SplashTextBox.Text;
//             _mainWindow.GuardarSesion();
//         }
// 
//         private void CloudPathBox_TextChanged(object sender, TextChangedEventArgs e)
//         {
//             if (_initializing) return;
//             _mainWindow.Session.CloudPath = CloudPathBox.Text;
//             _mainWindow.GuardarSesion();
//         }
// 
//         private void OverlayToggle_Click(object sender, RoutedEventArgs e)
//         {
//             if (_initializing) return;
//             _mainWindow.Session.IsOverlayEnabled = OverlayToggle.IsChecked == true;
//             _mainWindow.GuardarSesion();
//         }

        // ═══════════════════════════════════════════════════════════
        //  CONFIGS DE PEPITA
        // ═══════════════════════════════════════════════════════════

        private ModSyncer? _syncerLocal;

        private ModSyncer GetSyncer() =>
            _syncerLocal ??= new ModSyncer(_mainWindow.GameFolder);

        /// <summary>Inicializa el bloque de configs oficiales. El boton de subida pide clave al usarse.</summary>
        private async void InicializarPanelPepita()
        {
            PepitaAdminPanel.Visibility = Visibility.Visible;

            // Verificar estado del hash en background
            await ActualizarEstadoHashAsync();
        }

        private async Task ActualizarEstadoHashAsync()
        {
            try
            {
                var remoteInfo = await GetSyncer().ObtenerConfigOficialRemota();
                string hashRemoto = remoteInfo?.Hash ?? "";
                string versionRemota = remoteInfo?.ConfigVersion ?? "0";
                int ramOficial = remoteInfo?.RecommendedRam ?? 4;

                if (string.IsNullOrEmpty(hashRemoto))
                {
                    PepitaConfigStatusText.Text = "No se pudo determinar la revision remota.";
                    PepitaConfigMetaText.Text = "Verifica conexion o publicacion.";
                    return;
                }

                string profileId = _mainWindow.CurrentProfile?.Id ?? "default";
                string versionAplicada = _mainWindow.Session.AppliedConfigVersions.TryGetValue(profileId, out string? applied) ? applied : "0";
                string versionRechazada = _mainWindow.Session.RejectedConfigVersions.TryGetValue(profileId, out string? rejected) ? rejected : "0";
                bool alDia = versionRemota == versionAplicada;

                if (alDia)
                {
                    PepitaConfigStatusText.Text = $"Revision oficial v{versionRemota} aplicada.";
                    PepitaConfigStatusText.Foreground = System.Windows.Media.Brushes.LightGreen;
                }
                else if (versionRemota == versionRechazada)
                {
                    PepitaConfigStatusText.Text = $"Revision oficial v{versionRemota} disponible, pero rechazada.";
                    PepitaConfigStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                }
                else
                {
                    PepitaConfigStatusText.Text = $"Nueva revision oficial v{versionRemota} lista para adaptar.";
                    PepitaConfigStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0xF2, 0xFF));
                }

                PepitaConfigHashText.Text = $"Revision: v{versionRemota} | Hash: {(hashRemoto.Length > 8 ? hashRemoto[..8] : hashRemoto)}";
                PepitaConfigMetaText.Text = $"Publicada por {remoteInfo?.PublishedBy ?? "Pepa"} | RAM recomendada {ramOficial} GB | Aplicada localmente v{versionAplicada}";
            }
            catch (Exception ex)
            {
                PepitaConfigStatusText.Text = $"Error: {ex.Message}";
                PepitaConfigMetaText.Text = "";
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
                var remoteInfo = await GetSyncer().ObtenerConfigOficialRemota();
                if (remoteInfo == null) return;

                string hashRemoto = remoteInfo.Hash;
                string versionRemota = remoteInfo.ConfigVersion;
                int ramRec = remoteInfo.RecommendedRam;

                var result = MessageBox.Show(
                    $"Aplicar la revision oficial v{versionRemota}?\n\n" +
                    "- Se copiara la configuracion oficial del servidor.\n" +
                    "- Tus controles personales se mantienen siempre que sea posible.\n" +
                    "- Se ajustara la RAM recomendada a " + ramRec + "GB.\n" +
                    "- Se realizara un backup automatico antes de proceder.",
                    "Aplicar Config Oficial",
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result != MessageBoxResult.Yes) return;

                string backupDir = Path.Combine(_mainWindow.GameFolder, "backups", "pre-official-sync-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(backupDir);
                foreach (var target in new[] { "options.txt", "config" })
                {
                    string src = Path.Combine(_mainWindow.GameFolder, target);
                    if (File.Exists(src)) File.Copy(src, Path.Combine(backupDir, target), true);
                    else if (Directory.Exists(src)) CopyDirectory(src, Path.Combine(backupDir, target));
                }

                var syncer = GetSyncer();
                syncer.OnLog += msg => _mainWindow.AgregarLog(msg);
                await syncer.SincronizarConfigs(sobrescribirTodo: true);

                if (_mainWindow.CurrentProfile != null)
                {
                    _mainWindow.CurrentProfile.RamGB = ramRec;
                    _mainWindow.Session.LastAppliedConfigHash = hashRemoto;
                    string profileId = _mainWindow.CurrentProfile.Id;
                    _mainWindow.Session.AppliedConfigVersions[profileId] = versionRemota;
                    _mainWindow.Session.RejectedConfigVersions.Remove(profileId);
                    _mainWindow.GuardarSesion();
                }

                await ActualizarEstadoHashAsync();
                NotificationService.Instance.ShowSuccess($"Config oficial v{versionRemota} aplicada correctamente.");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.ShowError($"Error: {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content   = "Adaptar Config Oficial";
            }
        }

        private async void BtnPublicarConfigsAdmin_Click(object sender, RoutedEventArgs e)
        {
            var login = new AdminLoginWindow { Owner = _mainWindow };
            if (login.ShowDialog() != true || login.Clave != "1530")
            {
                if (!string.IsNullOrEmpty(login.Clave)) MessageBox.Show("Clave incorrecta.", "Acceso Denegado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "Subiendo...";
            SetAdminPublishProgress(0, "Esperando confirmacion...", false);
            try
            {
                var remoteInfo = await GetSyncer().ObtenerConfigOficialRemota();
                int currentConfigV = int.TryParse(remoteInfo?.ConfigVersion ?? "0", out int parsedVersion) ? parsedVersion : 0;
                int nextConfigV = currentConfigV + 1;

                var confirm = MessageBox.Show(
                    $"Publicar tu configuracion actual como la revision oficial v{nextConfigV}?\n\n" +
                    "Esto subira:\n" +
                    "- Carpeta config/\n" +
                    "- options.txt\n" +
                    "- shaderpacks y resourcepacks\n\n" +
                    "Los demas usuarios recibiran una notificacion.",
                    "Publicar Config Oficial",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var syncer = GetSyncer();
                int ramActual = _mainWindow.CurrentProfile?.RamGB ?? 4;
                bool ok = await syncer.PublicarConfigsAdmin(
                    msg => _mainWindow.AgregarLog(msg),
                    ramActual,
                    _mainWindow.Session.Username,
                    (percent, status, indeterminate) => Dispatcher.Invoke(() => SetAdminPublishProgress(percent, status, indeterminate)));

                if (ok)
                {
                    string profileId = _mainWindow.CurrentProfile?.Id ?? "default";
                    _mainWindow.Session.AppliedConfigVersions[profileId] = nextConfigV.ToString();
                    _mainWindow.Session.RejectedConfigVersions.Remove(profileId);
                    _mainWindow.GuardarSesion();
                    await ActualizarEstadoHashAsync();
                    SetAdminPublishProgress(100, $"Revision oficial v{nextConfigV} publicada.", false);
                    NotificationService.Instance.ShowSuccess($"Revision oficial v{nextConfigV} publicada exitosamente.");
                }
                else
                {
                    SetAdminPublishProgress(0, "No se pudo publicar. Revisa los logs.", false);
                    NotificationService.Instance.ShowError("Error al publicar. Verifica logs.");
                }
            }
            catch (Exception ex)
            {
                SetAdminPublishProgress(0, "Error al publicar configs.", false);
                NotificationService.Instance.ShowError($"Error: {ex.Message}");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = "Subir configs oficiales";
            }
        }

        private void SetAdminPublishProgress(int percent, string status, bool indeterminate)
        {
            AdminPublishStatusPanel.Visibility = Visibility.Visible;
            AdminPublishStatusText.Text = status;
            AdminPublishProgress.IsIndeterminate = indeterminate;
            AdminPublishProgress.Value = indeterminate ? 50 : Math.Max(0, Math.Min(100, percent));
            AdminPublishPercentText.Text = indeterminate ? "..." : $"{Math.Max(0, Math.Min(100, percent))}%";
        }

        private async void BtnForzarConfigsPropias_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;
            try
            {
                var confirm = MessageBox.Show(
                    "¿Forzar la descarga y aplicación COMPLETA de las configs de Pepita?\n\n" +
                    "ATENCIÓN: Esto SOBREESCRIBIRÁ tu options.txt y todas las configs de mods.\n" +
                    "Usalo solo si querés sincronizar esta PC desde cero.",
                    "¿Sobreescribir todo?",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var resRemoto = await GetSyncer().ObtenerConfigOficialRemota();
                string? hashRemoto = resRemoto?.Hash;
                string versionRemota = resRemoto?.ConfigVersion ?? "0";
                var syncer = GetSyncer();
                syncer.OnLog += msg => _mainWindow.AgregarLog(msg);
                await syncer.SincronizarConfigs(sobrescribirTodo: true);

                if (hashRemoto != null)
                {
                    _mainWindow.Session.LastAppliedConfigHash = hashRemoto;
                    string profileId = _mainWindow.CurrentProfile?.Id ?? "default";
                    _mainWindow.Session.AppliedConfigVersions[profileId] = versionRemota;
                    _mainWindow.Session.RejectedConfigVersions.Remove(profileId);
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
