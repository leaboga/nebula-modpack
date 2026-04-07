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

        public event Action<string>? OnLog;
        public event Action<double>? OnProgress;
        public event Action<string>? OnProgressLabel;

        public McGameLauncher(string gameFolder, int ramGB, string username, bool isPremium, string minecraftVersion, string loaderType, string loaderVersion, MSession? session = null, string? manualJavaPath = null)
        {
            _gameFolder = gameFolder;
            _ramGB = ramGB;
            _minecraftVersion = minecraftVersion;
            _loaderType = loaderType;
            _loaderVersion = loaderVersion;
            
            // Use modern offline session creation
            _session = session ?? MSession.CreateOfflineSession(username);
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

                if (_loaderType.ToLower().Contains("fabric"))
                {
                    finalVersionId = await InstalarFabric(path);
                }
                else if (_loaderType.ToLower().Contains("neoforge"))
                {
                    finalVersionId = await InstalarNeoForge(launcher);
                }
                else if (_loaderType.ToLower().Contains("forge"))
                {
                    finalVersionId = await InstalarForge(launcher);
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
                if (ex.InnerException != null) OnLog?.Invoke("  ↪ Detalle: " + ex.InnerException.Message);
                return -1;
            }
        }

        private async Task<string> InstalarNeoForge(CmlLib.Core.MinecraftLauncher launcher)
        {
             OnLog?.Invoke("⚒ Conectando con NeoForge Maven...");
             try {
                var type = Type.GetType("CmlLib.Core.Installer.NeoForge.NeoForgeInstaller, CmlLib.Core.Installer.NeoForge") ?? 
                           Type.GetType("CmlLib.Core.Installer.NeoForge.NeoForgeInstaller, CmlLib.Core");
                
                if (type == null) throw new Exception("Instalador de NeoForge no encontrado.");
                
                var handler = Activator.CreateInstance(type, launcher);
                return await InvokeInstallAsync(type, handler, _minecraftVersion, _loaderVersion);
             } catch (Exception ex) {
                OnLog?.Invoke("⚠ Fallo NeoForge: " + ex.Message + ". Reintentando con Forge...");
                return await InstalarForge(launcher);
             }
        }

        private async Task<string> InstalarForge(CmlLib.Core.MinecraftLauncher launcher)
        {
             OnLog?.Invoke("⚒ Conectando con Forge Maven...");
             try {
                var type = Type.GetType("CmlLib.Core.Installer.Forge.ForgeInstaller, CmlLib.Core.Installer.Forge") ??
                           Type.GetType("CmlLib.Core.Installer.Forge.ForgeInstaller, CmlLib.Core");

                if (type == null) throw new Exception("Instalador de Forge no encontrado.");

                var handler = Activator.CreateInstance(type, launcher);
                OnLog?.Invoke($"📥 Descargando e instalando Forge {_loaderVersion}...");
                return await InvokeInstallAsync(type, handler, _minecraftVersion, _loaderVersion);
             } catch (Exception ex) {
                throw new Exception("Error en instalador de Forge: " + ex.Message);
             }
        }

        private async Task<string> InvokeInstallAsync(Type type, object? instance, string mcVersion, string loaderVersion)
        {
            if (instance == null) throw new Exception("No se pudo crear instancia del instalador.");

            var methods = type.GetMethods()
                .Where(m => (m.Name == "InstallAsync" || m.Name == "Install"))
                .ToList();

            // 1. Prioritize method with (string, string) as first parameters
            var targetMethod = methods.FirstOrDefault(m => {
                var p = m.GetParameters();
                return p.Length >= 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(string);
            });

            object?[]? args = null;

            if (targetMethod != null)
            {
                var parameters = targetMethod.GetParameters();
                args = new object?[parameters.Length];
                args[0] = mcVersion;
                args[1] = loaderVersion;
                for (int i = 2; i < parameters.Length; i++) {
                    var def = parameters[i].DefaultValue;
                    args[i] = (def == DBNull.Value) ? null : def;
                }
            }
            else
            {
                // 2. Fallback: try any method with at least 2 params and attempt conversion
                foreach (var m in methods.OrderBy(m => m.GetParameters().Length))
                {
                    try {
                        var parameters = m.GetParameters();
                        if (parameters.Length < 2) continue;
                        args = new object?[parameters.Length];
                        args[0] = Convert.ChangeType(mcVersion, parameters[0].ParameterType);
                        args[1] = Convert.ChangeType(loaderVersion, parameters[1].ParameterType);
                        for (int i = 2; i < parameters.Length; i++) {
                            var def = parameters[i].DefaultValue;
                            args[i] = (def == DBNull.Value) ? null : def;
                        }
                        targetMethod = m;
                        break;
                    } catch { continue; }
                }
            }

            if (targetMethod == null || args == null) {
                var sigs = string.Join(", ", methods.Select(m => $"({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})"));
                throw new Exception($"No se encontró firma compatible en {type.Name}. Disponibles: {sigs}");
            }

            var result = targetMethod.Invoke(instance, args);
            if (result is Task<string> task) return await task;
            return result?.ToString() ?? "";
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
            dynamic handler = new FabricInstaller(new HttpClient());
            
            var loadersRes = await handler.GetLoaderVersionsAsync();
            IEnumerable<dynamic> loaders = loadersRes;
            var latest = loaders.FirstOrDefault()?.Version;
            if (string.IsNullOrEmpty(latest)) throw new Exception("No se encontraron cargadores Fabric.");
            
            OnLog?.Invoke($"📥 Instalando Fabric Loader {latest}...");
            await handler.InstallAsync(_minecraftVersion, latest, path);
            return $"fabric-loader-{latest}-{_minecraftVersion}";
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