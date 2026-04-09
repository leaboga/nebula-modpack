using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaLauncher
{
    public class VersionsIndex {
        public string LatestVersion { get; set; } = "";
        public List<VersionEntry> AvailableVersions { get; set; } = new();
    }
    public class VersionEntry {
        public string Version { get; set; } = "";
        public string Label { get; set; } = "";
        public string ManifestUrl { get; set; } = "";
    }
    public class ModManifest {
        public string Version { get; set; } = "";
        public string MinecraftVersion { get; set; } = "";
        public string Modloader { get; set; } = "";
        public string ModloaderVersion { get; set; } = "";
        public List<ModEntry> Mods { get; set; } = new();
        public bool ForceConfigUpdate { get; set; } = false;
        public string ConfigHash { get; set; } = "";
    }
    public class ModEntry {
        public string Name { get; set; } = "";
        public string Filename { get; set; } = "";
        public string Url { get; set; } = "";
        public string Md5 { get; set; } = "";
    }

    public class ModSyncer
    {
        private const string VersionsIndexUrl = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/versions-index.json";
        private const string AssetsUrl        = "https://github.com/leaboga/nebula-modpack/releases/download/client-assets-1.0/client-assets.zip";
        private readonly HttpClient _http = new HttpClient();
        private readonly string _modsFolder;
        private readonly string _gameFolder;

        public event Action<string>? OnLog;
        public event Action<double>? OnProgress;
        public event Action<string>? OnProgressLabel;

        public ModSyncer(string gameFolder) {
            _gameFolder = gameFolder;
            _modsFolder = Path.Combine(gameFolder, "mods");
            Directory.CreateDirectory(_modsFolder);
            _http.Timeout = TimeSpan.FromSeconds(5);
            _http.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher/5.0");
        }

        public async Task<VersionsIndex?> ObtenerVersionsIndex() {
            try {
                string json = await _http.GetStringAsync(VersionsIndexUrl + "?t=" + DateTime.Now.Ticks);
                var index = JsonConvert.DeserializeObject<VersionsIndex>(json);
                if (index != null) {
                    // Logic: If we are using a modpack-X.Y.Z tag for the index, we must also use it for the manifests
                    // unless they are explicitly pointing elsewhere. This fixes the 'main' branch sync issues.
                    string currentTag = VersionsIndexUrl.Contains("/modpack-") 
                        ? VersionsIndexUrl.Split("/modpack-")[1].Split('/')[0] 
                        : "main";

                    if (currentTag != "main")
                    {
                        foreach(var v in index.AvailableVersions) {
                            if (v.ManifestUrl.Contains("/main/")) 
                                v.ManifestUrl = v.ManifestUrl.Replace("/main/", "/modpack-" + currentTag + "/");
                            
                            if (v.ManifestUrl.Contains("/client-assets-1.0/")) 
                                v.ManifestUrl = v.ManifestUrl.Replace("/client-assets-1.0/", "/modpack-" + currentTag + "/");
                        }
                    }
                }
                return index;
            } catch (Exception ex) { OnLog?.Invoke("Error Index: " + ex.Message); return null; }
        }

        public async Task<ModManifest?> ObtenerManifest(string manifestUrl) {
            try {
                string json = await _http.GetStringAsync(manifestUrl + "?t=" + DateTime.Now.Ticks);
                var manifest = JsonConvert.DeserializeObject<ModManifest>(json);
                if (manifest != null) {
                    bool fixedAny = false;
                    foreach(var m in manifest.Mods) {
                        if (m.Url.Contains("/TU-USUARIO/")) {
                            m.Url = m.Url.Replace("/TU-USUARIO/", "/leaboga/");
                            fixedAny = true;
                        }
                        if (m.Url.Contains("/modpack-1.0.0/")) {
                            m.Url = m.Url.Replace("/modpack-1.0.0/", "/modpack-1.0.1/");
                            fixedAny = true;
                        }
                    }
                    if (fixedAny) OnLog?.Invoke("✨ Centinela: Se han corregido URLs de GitHub automáticamente.");
                }
                return manifest;
            } catch (Exception ex) { OnLog?.Invoke("Error Manifest: " + ex.Message); return null; }
        }

        public async Task<bool> SincronizarMods(ModManifest manifest)
        {
            try {
                OnLog?.Invoke($"Iniciando Nebula v{manifest.Version}");
                var requiredMods = manifest.Mods;
                
                if (Directory.Exists(_modsFolder)) {
                    foreach (var file in Directory.GetFiles(_modsFolder, "*.jar")) {
                        string currentFile = Path.GetFileName(file);
                        if (!requiredMods.Exists(m => m.Filename == currentFile) || currentFile.Contains("mapped_moj")) {
                            OnLog?.Invoke($"  🗑 Eliminando archivo conflictivo: {currentFile}");
                            File.Delete(file);
                        }
                    }
                }

                int total = requiredMods.Count;
                for (int i = 0; i < total; i++) {
                    var mod = requiredMods[i];
                    string localPath = Path.Combine(_modsFolder, mod.Filename);
                    
                    if (File.Exists(localPath)) {
                        string localMd5 = CalcularMD5(localPath);
                        // Skip MD5 check if the manifest has a placeholder (like a1b2c3d4...)
                        if (mod.Md5.StartsWith("a1b2c3d4") || localMd5.Equals(mod.Md5, StringComparison.OrdinalIgnoreCase)) {
                            OnProgress?.Invoke((double)(i + 1) / total * 100);
                            continue; 
                        }
                        OnLog?.Invoke($"  🔄 Actualizando mod: {mod.Filename} (MD5 mismatch)");
                        File.Delete(localPath);
                    }

                    OnLog?.Invoke($"  ⬇ [{i+1}/{total}] {mod.Filename}");
                    OnProgressLabel?.Invoke($"Descargando: {mod.Filename}");
                    
                    if (!await DescargarConStream(mod.Url, localPath)) {
                        OnLog?.Invoke($"✗ Error crítico descargando {mod.Filename}.");
                        return false;
                    }
                    OnProgress?.Invoke((double)(i + 1) / total * 100);
                }
                
                // --- ARREGLO DE CÁMARA ---
                CorregirConfigsDeCamara();
                
                OnLog?.Invoke("✓ Pack de mods actualizado correctamente.");
                return true;
            } catch (Exception ex) { OnLog?.Invoke("Error Sincro: " + ex.Message); return false; }
        }

        private void CorregirConfigsDeCamara()
        {
            try {
                string configDir = Path.Combine(_gameFolder, "config");
                if (!Directory.Exists(configDir)) return;

                // Lista de archivos de configuración de mods de cámara comunes que dan problemas
                string[] cameraConfigs = { 
                    "firstperson.json", 
                    "realcamera.toml", 
                    "freecam.json", 
                    "camerautils.json",
                    "betterthirdperson.toml" 
                };

                foreach (var config in cameraConfigs)
                {
                    string path = Path.Combine(configDir, config);
                    if (File.Exists(path))
                    {
                        OnLog?.Invoke($"🔧 Reseteando cámara conflictiva: {config}");
                        File.Delete(path); // Forzamos a que el mod cree una config limpia al iniciar
                    }
                }
            } catch { }
        }

        public async Task SincronizarConfigs() {
            try {
                string url = AssetsUrl;
                string tempZip = Path.Combine(Path.GetTempPath(), "nebula_assets.zip");
                if (await DescargarConStream(url, tempZip)) {
                    ZipFile.ExtractToDirectory(tempZip, _gameFolder, true);
                    File.Delete(tempZip);
                    
                    // Re-corregir después de extraer los assets del servidor
                    CorregirConfigsDeCamara();
                    
                    OnLog?.Invoke("✓ Ajustes visuales cargados.");
                }
            } catch { }
        }

        private async Task<bool> DescargarConStream(string url, string dest) {
            int intentos = 3;
            while (intentos > 0) {
                try {
                    string finalUrl = url;
                    if (!finalUrl.Contains("?")) finalUrl += "?v=" + DateTime.Now.Ticks;
                    else finalUrl += "&v=" + DateTime.Now.Ticks;

                    using var response = await _http.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode) throw new Exception(response.StatusCode.ToString());
                    
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fileStream);
                    return true;
                } catch (Exception) {
                    intentos--;
                    if (File.Exists(dest)) File.Delete(dest);
                    if (intentos > 0) await Task.Delay(2000);
                }
            }
            return false;
        }

        private string CalcularMD5(string path) {
            try {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(path);
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            } catch { return "null"; }
        }
    }
}
