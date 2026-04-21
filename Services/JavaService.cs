using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace KrakenLauncher.Services
{
    public class JavaRuntime
    {
        public string Path { get; set; } = "";
        public string Version { get; set; } = "";
        public string Architecture { get; set; } = "";
        public bool IsRecommended { get; set; } = false;

        public override string ToString() => $"{Version} ({Architecture}) - {Path}";
    }

    public class JavaService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string[] SearchPaths = new[]
        {
            @"C:\Program Files\Java",
            @"C:\Program Files (x86)\Java",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Adoptium")
        };

        public static List<JavaRuntime> DetectRuntimes()
        {
            var runtimes = new List<JavaRuntime>();
            var pathsToScan = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Check common install directories
            foreach (var root in SearchPaths)
            {
                if (Directory.Exists(root))
                {
                    foreach (var dir in Directory.GetDirectories(root))
                    {
                        string bin = Path.Combine(dir, "bin", "java.exe");
                        if (File.Exists(bin)) pathsToScan.Add(bin);
                    }
                }
            }

            // 2. Check Registry
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\JavaSoft\JDK");
                if (key != null)
                {
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        using var subkey = key.OpenSubKey(subkeyName);
                        string? path = subkey?.GetValue("JavaHome")?.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            string bin = Path.Combine(path, "bin", "java.exe");
                            if (File.Exists(bin)) pathsToScan.Add(bin);
                        }
                    }
                }
            }
            catch { }

            // 3. Check PATH environment variable
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var p in pathEnv.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    try {
                        string bin = Path.Combine(p, "java.exe");
                        if (File.Exists(bin)) pathsToScan.Add(bin);
                    } catch { }
                }
            }

            // 4. Resolve versions
            foreach (var bin in pathsToScan)
            {
                var rt = GetRuntimeInfo(bin);
                if (rt != null) runtimes.Add(rt);
            }

            return runtimes.OrderByDescending(r => r.Version).ToList();
        }

        private static JavaRuntime? GetRuntimeInfo(string path)
        {
            try
            {
                var psi = new ProcessStartInfo(path, "-version")
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return null;

                string output = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                // Simple version parsing from java -version
                // Example: "openjdk version \"17.0.7\" 2023-04-18"
                string version = "Desconocida";
                var match = System.Text.RegularExpressions.Regex.Match(output, @"version ""([^""]+)""");
                if (match.Success) version = match.Groups[1].Value;

                string arch = output.Contains("64-Bit") ? "x64" : "x86";

                return new JavaRuntime
                {
                    Path = path,
                    Version = version,
                    Architecture = arch,
                    IsRecommended = version.StartsWith("17") || version.StartsWith("21") // Recommended for modern MC
                };
            }
            catch { return null; }
        }
        #region AUTO-SYSTEM ENSURE
        public static async Task<string> EnsureJavaAsync(int version, Action<string>? onLog = null, Action<double>? onProgress = null)
        {
            onLog?.Invoke($"🔍 Buscando entorno Java {version} compatible...");
            var runtimes = DetectRuntimes();
            
            // Try to find exact or compatible
            var best = runtimes.FirstOrDefault(r => r.Version.StartsWith(version.ToString()));
            if (best != null)
            {
                onLog?.Invoke($"✅ Java {version} detectado en: {best.Path}");
                return best.Path;
            }

            // Fallback: Download from Adoptium
            onLog?.Invoke($"🚀 No se encontró Java {version}. Iniciando descarga de Adoptium...");
            return await DownloadJavaAsync(version, onLog, onProgress);
        }

        private static async Task<string> DownloadJavaAsync(int version, Action<string>? onLog, Action<double>? onProgress)
        {
            string baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KrakenLauncher", "java", $"v{version}");
            Directory.CreateDirectory(baseDir);
            
            string binPath = Path.Combine(baseDir, "bin", "java.exe");
            if (File.Exists(binPath)) return binPath;

            // Adoptium API URL (Simplified for Windows x64)
            string feature = version == 8 ? "8" : version.ToString();
            string apiUrl = $"https://api.adoptium.net/v3/binary/latest/{feature}/ga/windows/x64/jdk/hotspot/normal/eclipse?project=jdk";

            onLog?.Invoke($"📥 Descargando OpenJDK {version} (Binary)...");
            
            string zipPath = Path.Combine(baseDir, "java_temp.zip");
            using (var response = await _httpClient.GetAsync(apiUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? 100_000_000;
                
                using (var fs = new FileStream(zipPath, FileMode.Create))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[8192];
                    long read = 0;
                    int bytesRead;
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead);
                        read += bytesRead;
                        onProgress?.Invoke((double)read / total * 100);
                    }
                }
            }

            onLog?.Invoke("📦 Descomprimiendo binários...");
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, baseDir, true);
            File.Delete(zipPath);

            // Find java.exe in the extracted folder (it might be in a subfolder like jdk-17.0.7+7)
            var subDirs = Directory.GetDirectories(baseDir);
            if (subDirs.Length > 0)
            {
                string found = Path.Combine(subDirs[0], "bin", "java.exe");
                if (File.Exists(found)) return found;
            }

            return binPath;
        }
        #endregion
    }
}
