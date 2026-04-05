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
            _session = session ?? new MSession(username, "token", "uuid");
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
                process.StartInfo.CreateNoWindow = true;

                OnLog?.Invoke("🌌 Traspasando horizonte de sucesos...");
                var gameProcess = Process.Start(process.StartInfo);
                if (gameProcess == null) return -1;

                try { gameProcess.PriorityClass = ProcessPriorityClass.High; } catch { }

                gameProcess.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) OnLog?.Invoke($"[DEBUG] {e.Data}"); };
                gameProcess.BeginErrorReadLine();

                await Task.Run(() => gameProcess.WaitForExit());
                return gameProcess.ExitCode;
            } catch (Exception ex) {
                OnLog?.Invoke("✗ Error de Transmisión: " + ex.Message);
                return -1;
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
                new MArgument("-Dsun.java2d.noddraw=true")
            });
            opt.ExtraJvmArguments = jvmArgs;
        }

        private async Task<string> InstalarFabric(MinecraftPath path)
        {
            OnLog?.Invoke("🧵 Sincronizando con Fabric API...");
            var fabricHandler = new FabricInstaller(new HttpClient());
            dynamic handler = fabricHandler;
            
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
             // Bypass compile-time checking for Forge as well to avoid namespace issues
             var forgeHandler = Activator.CreateInstance(Type.GetType("CmlLib.Core.Installer.Forge.ForgeInstaller, CmlLib.Core.Installer.Forge") ?? typeof(object), new HttpClient());
             
             OnLog?.Invoke($"📥 Descargando e instalando Forge {_neoforgeVersion}...");
             dynamic handler = forgeHandler ?? throw new Exception("Instalador de Forge no encontrado.");
             return await handler.InstallAsync(_minecraftVersion, _neoforgeVersion, path);
        }

        private async Task<string> GetJavaPath()
        {
            if (!string.IsNullOrEmpty(_manualJavaPath) && File.Exists(_manualJavaPath)) return _manualJavaPath;
            
            int version = 17;
            try {
               double ver = double.Parse(_minecraftVersion.Replace(".", "").Substring(0, Math.Min(3, _minecraftVersion.Replace(".", "").Length)));
               if (ver < 116) version = 8;
               else if (ver >= 1205) version = 21;
            } catch { }

            string javaRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher", "runtime", $"java{version}");
            string binPath = Path.Combine(javaRoot, "bin", "java.exe");
            if (File.Exists(binPath)) return binPath;
            return "java.exe";
        }
    }
}