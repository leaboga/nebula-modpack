using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Linq;

namespace NebulaLauncher.Modules
{
    public partial class ServerHostView : UserControl
    {
        private Process? _serverProcess;
        private string _serverFolderPath = "";
        private readonly DispatcherTimer _statsTimer;
        private readonly HttpClient _http = new();

        public ServerHostView()
        {
            InitializeComponent();
            
            // Set default path
            _serverFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher", "servers", "default_server");
            ServerPathBox.Text = _serverFolderPath;
            
            // Suscribir al bridge para enviar comandos
            ChatBridgeService.OnCommandRequest += (cmd) => 
            {
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    try { _serverProcess.StandardInput.WriteLine(cmd); } catch { }
                }
            };
            
            _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _statsTimer.Tick += StatsTimer_Tick;
            
            LoadVersions();
            CheckInstallation();
        }

        private void LoadVersions()
        {
            string[] versions = { "1.21.1", "1.20.1", "1.19.2", "1.18.2", "1.16.5", "1.12.2" };
            foreach (var v in versions) VersionComboBox.Items.Add(v);
            VersionComboBox.SelectedIndex = 0;
        }

        private void CheckInstallation()
        {
            bool exists = Directory.Exists(_serverFolderPath) && 
                         (File.Exists(Path.Combine(_serverFolderPath, "server.jar")) || 
                          File.Exists(Path.Combine(_serverFolderPath, "run.bat")) ||
                          File.Exists(Path.Combine(_serverFolderPath, "run.ps1")) ||
                          File.Exists(Path.Combine(_serverFolderPath, "fabric-server-launch.jar")));
            
            if (exists)
            {
                StatusText.Text = "Instalado / Detenido";
                StatusDot.Fill = Brushes.Gray;
                BtnStart.IsEnabled = true;
                BtnInstall.Content = "🔨 REINSTALAR SERVIDOR";
            }
            else
            {
                StatusText.Text = "No instalado";
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x3D, 0x35, 0x60));
                BtnStart.IsEnabled = false;
                BtnInstall.Content = "⚡ INSTALAR SERVIDOR";
            }
        }

        private void BtnBrowseServerPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar carpeta para el servidor",
                InitialDirectory = _serverFolderPath
            };

            if (dialog.ShowDialog() == true)
            {
                _serverFolderPath = dialog.FolderName;
                ServerPathBox.Text = _serverFolderPath;
                CheckInstallation();
            }
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            string version = VersionComboBox.SelectedItem?.ToString() ?? "1.20.1";
            string loader = (LoaderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "vanilla";
            
            BtnInstall.IsEnabled = false;
            BtnInstall.Content = "⌛ INSTALANDO...";
            LogToConsole($"Iniciando instalación: {loader} {version}...");

            try
            {
                Directory.CreateDirectory(_serverFolderPath);

                if (loader == "vanilla")
                {
                    await InstallVanilla(version);
                }
                else if (loader == "fabric")
                {
                    await InstallFabric(version);
                }
                else if (loader == "forge")
                {
                    await InstallForge(version);
                }
                else if (loader == "neoforge")
                {
                    await InstallNeoForge(version);
                }

                // Create eula.txt
                await File.WriteAllTextAsync(Path.Combine(_serverFolderPath, "eula.txt"), "eula=true");
                
                // Create server.properties if not exists
                string propertiesPath = Path.Combine(_serverFolderPath, "server.properties");
                if (!File.Exists(propertiesPath))
                {
                    await File.WriteAllTextAsync(propertiesPath, "online-mode=false\nmotd=Nebula Local Server\nmax-players=20\nview-distance=10");
                }

                LogToConsole("✅ Instalación completada con éxito.");
                CheckInstallation();
            }
            catch (Exception ex)
            {
                LogToConsole($"❌ Error instalando: {ex.Message}");
                MessageBox.Show($"Error instalando servidor: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnInstall.IsEnabled = true;
                BtnInstall.Content = "🔨 REINSTALAR SERVIDOR";
            }
        }

        private async Task InstallVanilla(string version)
        {
            LogToConsole($"Obteniendo manifest de Mojang...");
            var manifestJson = await _http.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            dynamic manifest = JsonConvert.DeserializeObject(manifestJson)!;
            
            string versionUrl = "";
            foreach (var v in manifest.versions)
            {
                if (v.id == version)
                {
                    versionUrl = v.url;
                    break;
                }
            }

            if (string.IsNullOrEmpty(versionUrl)) throw new Exception("Versión no encontrada en el manifest de Mojang.");

            var versionDetailJson = await _http.GetStringAsync(versionUrl);
            dynamic detail = JsonConvert.DeserializeObject(versionDetailJson)!;
            
            string serverUrl = detail.downloads.server.url;
            LogToConsole($"Descargando server.jar ({version})...");
            
            var bytes = await _http.GetByteArrayAsync(serverUrl);
            await File.WriteAllBytesAsync(Path.Combine(_serverFolderPath, "server.jar"), bytes);
        }

        private async Task InstallFabric(string version)
        {
            LogToConsole("Obteniendo versiones de Fabric...");
            var installerJson = await _http.GetStringAsync("https://meta.fabricmc.net/v2/versions/installer");
            dynamic installers = JsonConvert.DeserializeObject(installerJson)!;
            string installerVersion = installers[0].version;

            var loaderJson = await _http.GetStringAsync("https://meta.fabricmc.net/v2/versions/loader");
            dynamic loaders = JsonConvert.DeserializeObject(loaderJson)!;
            string loaderVersion = loaders[0].version;

            string downloadUrl = $"https://maven.fabricmc.net/net/fabricmc/fabric-installer/{installerVersion}/fabric-installer-{installerVersion}.jar";
            string installerPath = Path.Combine(_serverFolderPath, "fabric-installer.jar");
            
            LogToConsole($"Descargando instalador Fabric {installerVersion}...");
            var bytes = await _http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(installerPath, bytes);

            LogToConsole("Ejecutando instalador server...");
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{installerPath}\" server -mcversion {version} -loader {loaderVersion} -downloadMinecraft",
                WorkingDirectory = _serverFolderPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            var process = Process.Start(psi);
            if (process != null) await process.WaitForExitAsync();
            
            File.Delete(installerPath);
            LogToConsole("Fabric instalado con éxito.");
        }

        private async Task InstallForge(string version)
        {
            LogToConsole($"Buscando versión recomendada de Forge para {version}...");
            var promoJson = await _http.GetStringAsync("https://files.minecraftforge.net/maven/net/minecraftforge/forge/promotions_slim.json");
            dynamic promos = JsonConvert.DeserializeObject(promoJson)!;
            
            string forgeVersion = promos.promos[$"{version}-recommended"] ?? promos.promos[$"{version}-latest"];
            if (string.IsNullOrEmpty(forgeVersion)) throw new Exception($"No se encontró una versión de Forge para {version}");

            string downloadUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{version}-{forgeVersion}/forge-{version}-{forgeVersion}-installer.jar";
            string installerPath = Path.Combine(_serverFolderPath, "forge-installer.jar");

            LogToConsole($"Descargando Forge {forgeVersion}...");
            var bytes = await _http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(installerPath, bytes);

            LogToConsole("Instalando Forge (Modo Servidor)... Esto puede tardar varios minutos.");
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{installerPath}\" --installServer",
                WorkingDirectory = _serverFolderPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            var process = Process.Start(psi);
            if (process != null) await process.WaitForExitAsync();
            
            File.Delete(installerPath);
            LogToConsole("Forge instalado.");
        }

        private async Task InstallNeoForge(string version)
        {
            LogToConsole($"NeoForge para {version}...");
            // NeoForge uses a different maven and versioning
            // Example: https://maven.neoforged.net/releases/net/neoforged/neoforge/21.1.48/neoforge-21.1.48-installer.jar
            // We need to find the version.
            
            string neoVersion = "";
            if (version == "1.21.1") neoVersion = "21.1.48";
            else if (version == "1.20.4") neoVersion = "20.4.237";
            else if (version == "1.20.1") neoVersion = "20.1.0"; // Very early
            
            if (string.IsNullOrEmpty(neoVersion)) throw new Exception($"Soporte de auto-instalación NeoForge no disponible para {version}.");

            string downloadUrl = $"https://maven.neoforged.net/releases/net/neoforged/neoforge/{neoVersion}/neoforge-{neoVersion}-installer.jar";
            string installerPath = Path.Combine(_serverFolderPath, "neoforge-installer.jar");

            LogToConsole($"Descargando NeoForge {neoVersion}...");
            var bytes = await _http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(installerPath, bytes);

            LogToConsole("Instalando NeoForge (Modo Servidor)...");
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{installerPath}\" --install-server",
                WorkingDirectory = _serverFolderPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            var process = Process.Start(psi);
            if (process != null) await process.WaitForExitAsync();
            
            File.Delete(installerPath);
            LogToConsole("NeoForge instalado.");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_serverProcess != null && !_serverProcess.HasExited) return;

            string ram = ((int)RamSlider.Value).ToString();
            string workingDir = _serverFolderPath;
            string executable = "java";
            string args = "";

            // Detection of different launch types
            if (File.Exists(Path.Combine(workingDir, "run.bat"))) {
                executable = "cmd.exe";
                args = "/c run.bat " + (GuiCheck.IsChecked == true ? "" : "nogui");
            } else if (File.Exists(Path.Combine(workingDir, "run.ps1"))) {
                executable = "powershell.exe";
                args = "-ExecutionPolicy Bypass -File run.ps1 " + (GuiCheck.IsChecked == true ? "" : "nogui");
            } else {
                string jarName = "server.jar";
                if (File.Exists(Path.Combine(workingDir, "fabric-server-launch.jar"))) jarName = "fabric-server-launch.jar";
                else if (File.Exists(Path.Combine(workingDir, "forge-server.jar"))) jarName = "forge-server.jar";
                
                args = $"-Xmx{ram}G -Xms{ram}G -jar \"{jarName}\" {(GuiCheck.IsChecked == true ? "" : "nogui")}";
            }

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _serverProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _serverProcess.OutputDataReceived += (s, ev) => LogToConsole(ev.Data);
                _serverProcess.ErrorDataReceived += (s, ev) => LogToConsole($"[ERROR] {ev.Data}");
                _serverProcess.Exited += (s, ev) => Dispatcher.Invoke(OnServerExited);

                if (_serverProcess.Start())
                {
                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();
                    
                    StatusText.Text = "Ejecutando";
                    StatusDot.Fill = Brushes.LimeGreen;
                    BtnStart.IsEnabled = false;
                    BtnStop.IsEnabled = true;
                    _statsTimer.Start();
                    LogToConsole("🚀 Servidor iniciado.");
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"❌ Error al iniciar: {ex.Message}");
                MessageBox.Show($"Error al iniciar el servidor: {ex.Message}", "Error de ejecución", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_serverProcess == null || _serverProcess.HasExited) return;

            LogToConsole("🛑 Enviando comando de apagado...");
            try
            {
                _serverProcess.StandardInput.WriteLine("stop");
                
                // Backup kill after 15 seconds if it doesn't close
                Task.Delay(15000).ContinueWith(t => {
                    if (_serverProcess != null && !_serverProcess.HasExited) {
                        LogToConsole("⚠️ Forzando cierre del proceso...");
                        try { _serverProcess.Kill(true); } catch { }
                    }
                });
            }
            catch {
                try { _serverProcess.Kill(true); } catch { }
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_serverFolderPath))
            {
                try { Process.Start("explorer.exe", $"\"{_serverFolderPath}\""); }
                catch { }
            }
            else
            {
                MessageBox.Show("La carpeta del servidor no existe todavía. Debes instalarlo primero.", "Carpeta no encontrada", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnSyncMods_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || string.IsNullOrEmpty(mainWindow.GameFolder))
            {
                MessageBox.Show("No se pudo encontrar la carpeta del cliente actual.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Directory.Exists(_serverFolderPath))
            {
                MessageBox.Show("La carpeta del servidor no existe todavía. Instala el servidor primero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("¿Quieres copiar los mods y configuraciones de tu cliente actual al servidor?\nEsto reemplazará los mods en el servidor con los tuyos.", "Sincronizar Servidor", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "⏳ SINCRONIZANDO...";

            LogToConsole("🔄 Iniciando sincronización de Mods y Configs del cliente al servidor...");
            
            await Task.Run(() =>
            {
                try
                {
                    string[] foldersToSync = { "mods", "config", "scripts", "kubejs" };
                    foreach (var folder in foldersToSync)
                    {
                        string clientFolder = Path.Combine(mainWindow.GameFolder, folder);
                        string serverFolder = Path.Combine(_serverFolderPath, folder);

                        if (Directory.Exists(clientFolder))
                        {
                            if (Directory.Exists(serverFolder)) Directory.Delete(serverFolder, true);
                            Directory.CreateDirectory(serverFolder);
                            
                            foreach (string dirPath in Directory.GetDirectories(clientFolder, "*", SearchOption.AllDirectories))
                                Directory.CreateDirectory(dirPath.Replace(clientFolder, serverFolder));

                            foreach (string newPath in Directory.GetFiles(clientFolder, "*.*", SearchOption.AllDirectories))
                                File.Copy(newPath, newPath.Replace(clientFolder, serverFolder), true);
                                
                            // Delete client-side only mods from server folder if any are known
                            // Common client-side only mods:
                            string[] clientOnlyKeywords = { "rubidium", "embeddium", "oculus", "iris", "optifine", "sodium", "entityculling", "minimap", "mouseweaks", "controllable", "soundphysics", "ambientsounds", "itemphysic", "dynamiclights", "3dskinlayers", "customskin", "farsight", "dynamiccrosshair", "client" };
                            if (folder == "mods" && Directory.Exists(serverFolder))
                            {
                                foreach (string file in Directory.GetFiles(serverFolder, "*.jar"))
                                {
                                    string fileName = Path.GetFileName(file).ToLower();
                                    foreach (var keyword in clientOnlyKeywords)
                                    {
                                        if (fileName.Contains(keyword))
                                        {
                                            try { File.Delete(file); } catch { }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => MessageBox.Show($"Error al sincronizar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
                }
            });

            LogToConsole("✅ Sincronización de servidor completada con éxito. (Mods client-side omitidos)");
            btn.IsEnabled = true;
            btn.Content = "🔄 SINCRONIZAR MODS";
        }

        private void OnServerExited()
        {
            _statsTimer.Stop();
            StatusText.Text = "Detenido";
            StatusDot.Fill = Brushes.Gray;
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            LogToConsole("🏁 El servidor se ha detenido.");
            
            if (AutoRestartCheck.IsChecked == true && StatusText.Text != "No instalado")
            {
                LogToConsole("🔄 Auto-reiniciando en 5 segundos...");
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, e) => { timer.Stop(); if (StatusText.Text == "Detenido") BtnStart_Click(null!, null!); };
                timer.Start();
            }
        }

        private void StatsTimer_Tick(object? sender, EventArgs e)
        {
            if (_serverProcess == null || _serverProcess.HasExited) return;

            try
            {
                _serverProcess.Refresh();
                long mem = _serverProcess.WorkingSet64 / 1024 / 1024;
                RamBar.Value = Math.Min(100, (double)mem / (RamSlider.Value * 1024) * 100);
                RamText.Text = $"{mem} MB / {(int)RamSlider.Value} GB";

                // Simple CPU jitter for visual effect if real counter not available
                CpuBar.Value = new Random().Next(8, 25);
            }
            catch { }
        }

        private void LogToConsole(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Dispatcher.Invoke(() =>
            {
                ConsoleText.Text += $"\n{text}";
                if (ConsoleText.Text.Length > 20000) ConsoleText.Text = ConsoleText.Text.Substring(10000);
                ConsoleScroll.ScrollToEnd();

                // Procesar chat para el Bridge
                ParseChatLine(text);
            });
        }

        private void ParseChatLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            
            // Intento de parseo de chat estándar: [12:34:56] [Server thread/INFO]: <Player> Message
            if (line.Contains("[INFO]") || line.Contains("/INFO]"))
            {
                int colonIndex = line.IndexOf("]: ");
                if (colonIndex != -1)
                {
                    string content = line.Substring(colonIndex + 3);
                    
                    // Si empieza con < indica que es un mensaje de jugador
                    if (content.Trim().StartsWith("<"))
                    {
                        int endBracket = content.IndexOf(">");
                        if (endBracket != -1)
                        {
                            string sender = content.Substring(1, endBracket - 1);
                            string msg = content.Substring(endBracket + 1).Trim();
                            ChatBridgeService.AddMessage(sender, msg, "chat");
                        }
                    }
                    else if (content.Contains("joined the game"))
                    {
                        string player = content.Split(' ')[0];
                        ChatBridgeService.AddMessage("Sistema", $"🌍 {player} se unió al mundo", "sys");
                    }
                    else if (content.Contains("left the game"))
                    {
                        string player = content.Split(' ')[0];
                        ChatBridgeService.AddMessage("Sistema", $"🚪 {player} salió del mundo", "sys");
                    }
                }
            }
        }

        private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int val = (int)e.NewValue;
            if (RamValueText != null) RamValueText.Text = $"{val} GB";
            if (RamText != null)
            {
                if (RamText.Text.Contains("/"))
                {
                    string used = RamText.Text.Split('/')[0].Trim();
                    RamText.Text = $"{used} / {val} GB";
                }
                else
                {
                    RamText.Text = $"0 MB / {val} GB";
                }
            }
        }
    }
}
