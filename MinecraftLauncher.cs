using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using CmlLib.Core.Installer;
using CmlLib.Core.ModLoaders.FabricMC;
using System.Linq;

namespace NebulaLauncher
{
    public class McGameLauncher
    {
        private readonly string _gameFolder;
        private readonly int _ramGB;
        private readonly string _minecraftVersion;
        private readonly string _neoforgeVersion;
        private readonly MSession _session;
        private readonly string? _manualJavaPath;

        public event Action<string>? OnLog;
        public event Action<double>? OnProgress;
        public event Action<string>? OnProgressLabel;

        public McGameLauncher(string gameFolder, int ramGB, string username, bool isPremium, string minecraftVersion, string loaderVersion, MSession? session = null, string? manualJavaPath = null)
        {
            _gameFolder = gameFolder;
            _ramGB = ramGB;
            _minecraftVersion = minecraftVersion;
            _neoforgeVersion = loaderVersion;
            
            // Fix: Modern Minecraft requires a valid UUID format even in offline mode.
            // Using a deterministic name-based UUID for offline mode.
            _session = session ?? MSession.GetOfflineSession(username);
            _manualJavaPath = manualJavaPath;
        }

        public async Task<int> LaunchAsync()
        {
            try
            {
                OnLog?.Invoke("🚀 Iniciando motores de curvatura...");
                var path = new MinecraftPath(_gameFolder);
                var launcher = new CmlLib.Core.MinecraftLauncher(path);
                
                string finalVersionId = _minecraftVersion;

                if (_neoforgeVersion.ToLower().Contains("fabric"))
                {
                    finalVersionId = await InstalarFabric(path);
                }
                else if (_neoforgeVersion.ToLower().Contains("neoforge"))
                {
                    string javaPath = await GetJavaPath();
                    finalVersionId = await InstalarNeoForge(path, javaPath);
                }
                else if (_neoforgeVersion.ToLower().Contains("forge"))
                {
                    string javaPath = await GetJavaPath();
                    finalVersionId = await InstalarForge(path, javaPath);
                }

                OnLog?.Invoke("📦 Sincronizando recursos base...");
                await launcher.InstallAsync(finalVersionId);

                var launchOption = new MLaunchOption
                {
                    MaximumRamMb = _ramGB * 1024,
                    Session = _session,
                    JavaPath = await GetJavaPath()
                };

                SetOptimizedArgs(launchOption);

                var process = await launcher.BuildProcessAsync(finalVersionId, launchOption);
                
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                OnLog?.Invoke("🌌 Traspasando horizonte de sucesos...");
                var gameProcess = Process.Start(process.StartInfo);
                if (gameProcess == null) return -1;

                try { gameProcess.PriorityClass = ProcessPriorityClass.High; } catch { }

                gameProcess.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) OnLog?.Invoke($"[LOG] {e.Data}"); };
                gameProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) OnLog?.Invoke($"[DEBUG] {e.Data}"); };
                
                gameProcess.BeginOutputReadLine();
                gameProcess.BeginErrorReadLine();

                await Task.Run(() => gameProcess.WaitForExit());
                return gameProcess.ExitCode;
            } catch (Exception ex) {
                OnLog?.Invoke("✗ Error de Transmisión: " + ex.Message);
                return -1;
            }
        }

        private async Task<string> InstalarNeoForge(MinecraftPath path, string javaPath)
        {
             OnLog?.Invoke("⚒ Conectando con NeoForge Maven...");
             try {
                // Try to use NeoForge installer if available in the library
                var type = Type.GetType("CmlLib.Core.Installer.NeoForge.NeoForgeInstaller, CmlLib.Core.Installer.NeoForge") ?? typeof(object);
                dynamic handler = Activator.CreateInstance(type, new HttpClient())!;
                return await handler.InstallAsync(_minecraftVersion, _neoforgeVersion, path);
             } catch {
                // Fallback to Forge installer if NeoForge fails (some versions are compatible)
                return await InstalarForge(path, javaPath);
             }
        }

        private void SetOptimizedArgs(MLaunchOption opt)
        {
            var jvmArgs = new List<MArgument>();
            jvmArgs.AddRange(new[] {
                new MArgument("-XX:+UseG1GC"),
                new MArgument("-XX:+UnlockExperimentalVMOptions"),
                new MArgument("-XX:MaxGCPauseMillis=40"),
                new MArgument("-XX:G1NewSizePercent=20"),
                new MArgument("-XX:G1ReservePercent=20"),
                new MArgument("-XX:G1HeapRegionSize=32M"),
                new MArgument("-XX:G1MixedGCCountTarget=8"),
                new MArgument("-XX:+AlwaysPreTouch"),
                new MArgument("-Dsun.java2d.noddraw=true"),
                new MArgument("-Djna.nosys=true") // Avoid some JNA conflicts
            });
            opt.ExtraJvmArguments = jvmArgs;
        }

        private async Task<string> InstalarFabric(MinecraftPath path)
        {
            OnLog?.Invoke("🧵 Sincronizando con Fabric API...");
            dynamic handler = new FabricInstaller(new HttpClient());
            
            var loadersRes = await handler.GetLoaderVersionsAsync();
            IEnumerable<dynamic> loaders = loadersRes;
            var latest = loaders.FirstOrDefault()?.Version;
            if (string.IsNullOrEmpty(latest)) throw new Exception("No se encontraron cargadores Fabric.");
            
            OnLog?.Invoke($"📥 Instalando Fabric Loader {latest}...");
            await handler.InstallAsync(_minecraftVersion, latest, path);
            return $"fabric-loader-{latest}-{_minecraftVersion}";
        }

        private async Task<string> InstalarForge(MinecraftPath path, string javaPath)
        {
             OnLog?.Invoke("⚒ Conectando con Forge Maven...");
             var type = Type.GetType("CmlLib.Core.Installer.Forge.ForgeInstaller, CmlLib.Core.Installer.Forge") ?? typeof(object);
             dynamic handler = Activator.CreateInstance(type, new HttpClient())!;
             
             OnLog?.Invoke($"📥 Descargando e instalando Forge {_neoforgeVersion}...");
             return await handler.InstallAsync(_minecraftVersion, _neoforgeVersion, path);
        }

        private async Task<string> GetJavaPath()
        {
            if (!string.IsNullOrEmpty(_manualJavaPath) && File.Exists(_manualJavaPath)) return _manualJavaPath;
            
            int version = 17;
            try {
               var parts = _minecraftVersion.Split('.');
               if (parts.Length >= 2) {
                   int minor = int.Parse(parts[1]);
                   int patch = parts.Length >= 3 ? int.Parse(parts[2]) : 0;
                   
                   if (minor <= 16) version = 8;
                   else if (minor <= 19) version = 17;
                   else if (minor == 20 && patch < 5) version = 17;
                   else version = 21;
               }
            } catch { }

            string javaRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher", "runtime", $"java{version}");
            string binPath = Path.Combine(javaRoot, "bin", "java.exe");
            if (File.Exists(binPath)) return binPath;
            return "java.exe";
        }
    }
}