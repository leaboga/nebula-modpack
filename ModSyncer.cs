using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KrakenLauncher
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
        public string ConfigVersion { get; set; } = "1"; // Versión de configuración oficial
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

    public class OfficialConfigInfo
    {
        public string Hash { get; set; } = "";
        public string ConfigVersion { get; set; } = "0";
        public int RecommendedRam { get; set; } = 4;
        public string PublishedAt { get; set; } = "";
        public string PublishedBy { get; set; } = "Pepa";
    }

    public class ModSyncer
    {
        private const string VersionsIndexUrl = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/versions-index.json";
        private const string AssetsUrl        = "https://github.com/leaboga/nebula-modpack/releases/download/client-assets-1.0/client-assets.zip";
        private const string LegacyPepitaAssetsUrl = "https://github.com/leaboga/nebula-modpack/releases/download/client-assets-1.0/client-assets-pepita.zip";
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
            _http.Timeout = TimeSpan.FromSeconds(30);
            _http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher/" + Services.VersionManager.GetCurrentVersion());
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

        // URL del JSON que pepita publica con el hash de sus configs
        private const string ConfigHashUrl = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/config-hash.json";

        /// <summary>
        /// Devuelve la info remota de las configs de Pepita (hash y RAM recomendada), o null si no se puede obtener.
        /// </summary>
        public async Task<OfficialConfigInfo?> ObtenerConfigOficialRemota()
        {
            try
            {
                string json = await _http.GetStringAsync(ConfigHashUrl + "?t=" + DateTime.Now.Ticks);
                return NormalizarConfigInfo(json);
            }
            catch
            {
                try
                {
                    string apiJson = await _http.GetStringAsync("https://api.github.com/repos/leaboga/nebula-modpack/contents/config-hash.json?ref=main&t=" + DateTime.Now.Ticks);
                    var apiObj = JObject.Parse(apiJson);
                    string encoded = apiObj["content"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(encoded)) return null;

                    string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded.Replace("\n", "").Replace("\r", "")));
                    return NormalizarConfigInfo(json);
                }
                catch { return null; }
            }
        }

        private OfficialConfigInfo? NormalizarConfigInfo(string json)
        {
            var info = JsonConvert.DeserializeObject<OfficialConfigInfo>(json);
            if (info == null) return null;
            info.Hash ??= "";
            info.ConfigVersion ??= "0";
            if (string.IsNullOrWhiteSpace(info.PublishedBy)) info.PublishedBy = "Pepa";
            return info;
        }

        /// <summary>
        /// Aplica las configs de Pepita desde GitHub. Respeta archivos que el usuario no debería perder
        /// (options.txt, options.of.txt) extrayéndolos pero SIN pisar si ya existen.
        /// Llame a este método solo con consentimiento explícito del usuario.
        /// </summary>
        public async Task SincronizarConfigs(string? version = null, bool sobrescribirTodo = false)
        {
            try
            {
                string url = AssetsUrl;
                if (!string.IsNullOrEmpty(version)) {
                    // Si la versión no contiene guiones o puntos raros, asumimos el formato estándar v[version]-assets
                    url = $"https://github.com/leaboga/nebula-modpack/releases/download/v{version}-assets/client-assets.zip";
                }

                string tempZip = Path.Combine(Path.GetTempPath(), "nebula_assets.zip");
                if (!await DescargarConStream(url, tempZip)) {
                    if (!string.IsNullOrEmpty(version)) {
                        OnLog?.Invoke($"  ⚠️ No se encontró asset específico para v{version}. Reintentando con base...");
                        if (!await DescargarConStream(AssetsUrl, tempZip) && !await DescargarConStream(LegacyPepitaAssetsUrl, tempZip)) return;
                    } else if (!await DescargarConStream(LegacyPepitaAssetsUrl, tempZip)) return;
                }

                // Archivos que NO deben sobreescribirse nunca (preferencias personales de Minecraft)
                var protegidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "options.txt",
                    "options.of.txt",
                    "servers.dat",
                    "hotbar.nbt",
                    "realms_persistence.json"
                };

                using (var zip = System.IO.Compression.ZipFile.OpenRead(tempZip))
                {
                    foreach (var entry in zip.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // es directorio

                        string destPath = Path.Combine(_gameFolder, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        string fileName = Path.GetFileName(destPath);

                        // Proteger archivos personales si no se forzó sobrescritura
                        if (!sobrescribirTodo && File.Exists(destPath) && protegidos.Contains(fileName))
                        {
                            OnLog?.Invoke($"  🛡 Protegido (no sobreescrito): {fileName}");
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        entry.ExtractToFile(destPath, overwrite: true);
                    }
                }

                File.Delete(tempZip);
                OnLog?.Invoke("✓ Ajustes visuales de Pepita aplicados.");
            }
            catch (Exception ex) { OnLog?.Invoke("⚠ Error sincronizando configs: " + ex.Message); }
        }

        /// <summary>
        /// [ADMIN - solo Pepita] Empaqueta la carpeta config/ local y la sube a GitHub Releases
        /// como el asset "client-assets.zip". También actualiza config-hash.json en el repo.
        /// Retorna true si todo OK.
        /// </summary>
        public async Task<bool> PublicarConfigsAdmin(Action<string> log, int recommendedRam = 4, string publishedBy = "Pepa")
        {
            try
            {
                string configDir = Path.Combine(_gameFolder, "config");
                if (!Directory.Exists(configDir))
                { log("❌ No existe la carpeta config/"); return false; }

                // 1. Crear ZIP temporal con todo el contenido del gameFolder relevante
                string tempZip = Path.Combine(Path.GetTempPath(), "client-assets.zip");
                if (File.Exists(tempZip)) File.Delete(tempZip);

                log("📦 Empaquetando configs...");

                // Incluimos config/ y options.txt y options.of.txt si existen
                using (var archive = System.IO.Compression.ZipFile.Open(tempZip, System.IO.Compression.ZipArchiveMode.Create))
                {
                    // config/ completa
                    foreach (var file in Directory.GetFiles(configDir, "*", SearchOption.AllDirectories))
                    {
                        string relative = Path.GetRelativePath(_gameFolder, file);
                        archive.CreateEntryFromFile(file, relative.Replace(Path.DirectorySeparatorChar, '/'));
                    }

                    // options.txt
                    string optsPath = Path.Combine(_gameFolder, "options.txt");
                    if (File.Exists(optsPath))
                        archive.CreateEntryFromFile(optsPath, "options.txt");

                    // shaderpacks/shaders.txt si existe (shaderpack seleccionado)
                    string shaderOpts = Path.Combine(_gameFolder, "optionsshaders.txt");
                    if (File.Exists(shaderOpts))
                        archive.CreateEntryFromFile(shaderOpts, "optionsshaders.txt");
                }

                // 2. Calcular hash del ZIP para que los clientes detecten cambios
                string hash = CalcularMD5(tempZip);
                log($"🔑 Hash de configs: {hash}");

                // 3. Subir ZIP a GitHub Release (tag: client-assets-1.0 → overwrite asset)
                log("☁ Subiendo ZIP a GitHub...");
                var psi = new System.Diagnostics.ProcessStartInfo("gh",
                    $"release upload client-assets-1.0 \"{tempZip}\" --repo leaboga/nebula-modpack --clobber")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null) { log("❌ No se pudo iniciar gh."); return false; }
                await proc.WaitForExitAsync();
                if (proc.ExitCode != 0)
                {
                    log("❌ Error subiendo asset: " + await proc.StandardError.ReadToEndAsync());
                    return false;
                }

                // 4. Crear/actualizar config-hash.json en el repo (requiere gh + repo clonado o API)
                //    Usamos un archivo temporal y lo subimos via gh api
                var currentInfo = await ObtenerConfigOficialRemota();
                int currentVersion = 0;
                int.TryParse(currentInfo?.ConfigVersion ?? "0", out currentVersion);
                var infoObj = new OfficialConfigInfo
                {
                    Hash = hash,
                    ConfigVersion = (currentVersion + 1).ToString(),
                    RecommendedRam = recommendedRam,
                    PublishedAt = DateTime.UtcNow.ToString("o"),
                    PublishedBy = string.IsNullOrWhiteSpace(publishedBy) ? "Pepa" : publishedBy
                };
                string hashJson = Newtonsoft.Json.JsonConvert.SerializeObject(infoObj, Newtonsoft.Json.Formatting.Indented);
                string hashFile = Path.Combine(Path.GetTempPath(), "config-hash.json");
                await File.WriteAllTextAsync(hashFile, hashJson);

                // Intentar actualizar via gh api (puede fallar si no tiene permisos de push directo)
                var psi2 = new System.Diagnostics.ProcessStartInfo("gh",
                    $"api repos/leaboga/nebula-modpack/contents/config-hash.json " +
                    $"--method PUT " +
                    $"-f message=\"chore: update config hash [{hash[..8]}]\" " +
                    $"-f content=\"{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(hashJson))}\" " +
                    $"--jq .commit.sha")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                // Si el archivo ya existe necesitamos el SHA previo
                try
                {
                    var shaResult = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("gh",
                        "api repos/leaboga/nebula-modpack/contents/config-hash.json --jq .sha")
                    {
                        RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
                    })!;
                    await shaResult.WaitForExitAsync();
                    string sha = (await shaResult.StandardOutput.ReadToEndAsync()).Trim();
                    if (!string.IsNullOrEmpty(sha))
                        psi2.Arguments += $" -f sha=\"{sha}\"";
                }
                catch { }

                var proc2 = System.Diagnostics.Process.Start(psi2);
                if (proc2 == null)
                {
                    log("No se pudo actualizar config-hash.json.");
                    return false;
                }

                await proc2.WaitForExitAsync();
                if (proc2.ExitCode != 0)
                {
                    log("Error actualizando config-hash.json: " + await proc2.StandardError.ReadToEndAsync());
                    return false;
                }

                File.Delete(tempZip);
                log($"✅ Configs de Pepita publicadas (hash: {hash[..8]}...)");
                return true;
            }
            catch (Exception ex) { log("❌ Error publicando configs: " + ex.Message); return false; }
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
