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
using System.Reflection;
using NebulaLauncher.Modules;

namespace NebulaLauncher
{
    public class McGameLauncher
    {
        private readonly string _gameFolder;
        private readonly int _ramGB;
        private readonly string _minecraftVersion;
        private readonly string _loaderType;
        private readonly string _loaderVersion;
        private readonly MSession _session;
        private readonly string? _manualJavaPath;
        private readonly string? _customSplash;
        private readonly bool _isOverlay;

        public event Action<string>? OnLog;
        public event Action<double>? OnProgress;
        public event Action<string>? OnProgressLabel;

        public McGameLauncher(string gameFolder, int ramGB, string username, bool isPremium, string minecraftVersion, string loaderType, string loaderVersion, MSession? session = null, string? manualJavaPath = null, string? customSplash = null, bool isOverlay = false)
        {
            _gameFolder = gameFolder;
            _ramGB = ramGB;
            _minecraftVersion = minecraftVersion;
            _loaderType = loaderType;
            _loaderVersion = loaderVersion;
            
            // Use modern offline session
            _session = session ?? MSession.CreateOfflineSession(username);
            _manualJavaPath = manualJavaPath;
            _customSplash = customSplash;
            _isOverlay = isOverlay;
        }

        public async Task<int> LaunchAsync()
        {
            try
            {
                OnLog?.Invoke("🚀 Iniciando motores de curvatura...");
                var path = new MinecraftPath(_gameFolder);
                var launcher = new CmlLib.Core.MinecraftLauncher(path);
                
                string finalVersionId = _minecraftVersion;

                string lType = _loaderType.ToLower();
                if (lType.Contains("fabric"))
                {
                    finalVersionId = await InstalarFabric(path);
                }
                else if (lType.Contains("neoforge"))
                {
                    finalVersionId = await InstalarConReflexion("NeoForge", launcher);
                }
                else if (lType.Contains("forge"))
                {
                    finalVersionId = await InstalarConReflexion("Forge", launcher);
                }

                OnLog?.Invoke("📦 Sincronizando recursos base...");
                await launcher.InstallAsync(finalVersionId);

                // Custom Splash
                if (!string.IsNullOrEmpty(_customSplash))
                {
                     try {
                         var cfg = new ConfigManager(_gameFolder);
                         await cfg.UpdateSplashText(_customSplash);
                     } catch { }
                }

                var launchOption = new MLaunchOption
                {
                    MaximumRamMb = _ramGB * 1024,
                    Session = _session,
                    JavaPath = await GetJavaPath()
                };

                SetOptimizedArgs(launchOption);
                
                if (_isOverlay)
                {
                    var jvmArgs = new List<MArgument>(launchOption.ExtraJvmArguments ?? Enumerable.Empty<MArgument>());
                    jvmArgs.Add(new MArgument("-Dnebula.overlay=true"));
                    launchOption.ExtraJvmArguments = jvmArgs;
                }

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
                if (ex.InnerException != null) OnLog?.Invoke("  ↪ Detalle: " + ex.InnerException.Message);
                return -1;
            }
        }

        private async Task<string> InstalarConReflexion(string engine, CmlLib.Core.MinecraftLauncher launcher)
        {
            OnLog?.Invoke($"⚒ Conectando con {engine} Maven...");
            
            // Try to find the installer type in multiple known assemblies/namespaces
            string[] typeNames = {
                $"CmlLib.Core.Installer.{engine}.{engine}Installer, CmlLib.Core.Installer.{engine}",
                $"CmlLib.Core.Installer.{engine}.{engine}Installer, CmlLib.Core",
                $"CmlLib.Core.Installer.{engine}Installer, CmlLib.Core"
            };

            Type? type = null;
            foreach (var name in typeNames) {
                type = Type.GetType(name);
                if (type != null) break;
            }

            if (type == null) {
                if (engine == "NeoForge") return await InstalarConReflexion("Forge", launcher);
                throw new Exception($"Instalador de {engine} no encontrado.");
            }

            var instance = Activator.CreateInstance(type, launcher);
            if (instance == null) throw new Exception($"No se pudo crear instancia de {engine}Installer.");

            OnLog?.Invoke($"📥 Descargando e instalando {engine} {_loaderVersion}...");

            // Find method by name and parameter count, prioritizing those that accept strings
            var methods = type.GetMethods()
                .Where(m => (m.Name == "InstallAsync" || m.Name == "Install") && m.GetParameters().Length >= 2)
                .OrderBy(m => m.GetParameters().Length)
                .ToList();

            Exception? lastEx = null;
            foreach (var method in methods)
            {
                try
                {
                    var pars = method.GetParameters();
                    var args = new object?[pars.Length];
                    
                    // First 2 are usually Minecraft version and Loader version
                    args[0] = _minecraftVersion;
                    args[1] = _loaderVersion;

                    // Fill remaining with defaults or new instances
                    for (int i = 2; i < pars.Length; i++) {
                        var pType = pars[i].ParameterType;
                        if (pars[i].HasDefaultValue) args[i] = pars[i].DefaultValue;
                        else if (pType.IsClass && pType != typeof(string)) {
                            try { args[i] = Activator.CreateInstance(pType); } catch { args[i] = null; }
                        }
                        else args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : null;
                    }

                    var result = method.Invoke(instance, args);
                    if (result is Task<string> task) return await task;
                    return result?.ToString() ?? "";
                }
                catch (Exception ex) { lastEx = ex; continue; }
            }

            throw new Exception($"Fallo en {engine}: " + (lastEx?.InnerException?.Message ?? lastEx?.Message ?? "Firma no compatible"));
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
                new MArgument("-Djna.nosys=true")
            });
            opt.ExtraJvmArguments = jvmArgs;
        }

        private async Task<string> InstalarFabric(MinecraftPath path)
        {
            OnLog?.Invoke("🧵 Sincronizando con Fabric API...");
            try {
                dynamic handler = new FabricInstaller(new HttpClient());
                
                var loadersRes = await handler.GetLoaderVersionsAsync();
                IEnumerable<dynamic> loaders = loadersRes;
                var latest = loaders.FirstOrDefault()?.Version;
                if (string.IsNullOrEmpty(latest)) throw new Exception("No se encontraron cargadores Fabric.");
                
                OnLog?.Invoke($"📥 Instalando Fabric Loader {latest}...");
                await handler.InstallAsync(_minecraftVersion, (string)latest, path);
                return $"fabric-loader-{latest}-{_minecraftVersion}";
            } catch (Exception ex) {
                throw new Exception("Error en instalador de Fabric: " + ex.Message);
            }
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