using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using NebulaLauncher.Services;

namespace NebulaLauncher.Services
{
    public static class JavaService
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };

        public static async Task<string> EnsureJavaAsync(int version, Action<string>? onLog = null, Action<double>? onProgress = null)
        {
            string javaRoot = Path.Combine(PathService.AppFolder, "runtime", $"java{version}");
            string binPath = Path.Combine(javaRoot, "bin", "java.exe");

            // Recalcular binPath si extraemos una carpeta anidada (común en Adoptium zips)
            if (!File.Exists(binPath))
            {
                // Intentar buscar recursivamente si existe la carpeta pero el binario no está en la raíz esperada
                if (Directory.Exists(javaRoot))
                {
                    var files = Directory.GetFiles(javaRoot, "java.exe", SearchOption.AllDirectories);
                    if (files.Length > 0) return files[0];
                }

                onLog?.Invoke($"☕ Java {version} no encontrado. Iniciando descarga desde Adoptium...");
                await DownloadAndExtractJava(version, javaRoot, onLog, onProgress);
                
                // Buscar de nuevo después de extraer
                var finalFiles = Directory.GetFiles(javaRoot, "java.exe", SearchOption.AllDirectories);
                if (finalFiles.Length > 0) return finalFiles[0];
                
                throw new Exception($"No se pudo encontrar java.exe después de la instalación de Java {version}.");
            }

            return binPath;
        }

        private static async Task DownloadAndExtractJava(int featureVersion, string targetFolder, Action<string>? onLog, Action<double>? onProgress)
        {
            string os = "windows";
            string arch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            string url = $"https://api.adoptium.net/v3/binary/latest/{featureVersion}/ga/{os}/{arch}/jdk/hotspot/normal/eclipse";

            onLog?.Invoke($"📥 Solicitando link de descarga para Java {featureVersion} ({arch})...");
            
            string tempZip = Path.Combine(Path.GetTempPath(), $"java{featureVersion}_setup.zip");
            
            using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    int read;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read);
                        totalRead += read;
                        if (totalBytes != -1)
                        {
                            onProgress?.Invoke((double)totalRead / totalBytes * 100);
                        }
                    }
                }
            }

            onLog?.Invoke("📦 Extrayendo entorno de ejecución Java...");
            if (Directory.Exists(targetFolder)) Directory.Delete(targetFolder, true);
            Directory.CreateDirectory(targetFolder);

            await Task.Run(() => ZipFile.ExtractToDirectory(tempZip, targetFolder));
            
            if (File.Exists(tempZip)) File.Delete(tempZip);
            onLog?.Invoke($"✅ Java {featureVersion} instalado correctamente.");
        }
    }
}
