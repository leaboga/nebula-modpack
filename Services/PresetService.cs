using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KrakenLauncher.Services
{
    public class PresetService
    {
        private readonly string _presetsFolder;

        public PresetService()
        {
            _presetsFolder = Path.Combine(PathService.AppFolder, "presets");
            Directory.CreateDirectory(_presetsFolder);
        }

        public class PresetMetadata
        {
            public string Name { get; set; } = "";
            public int VersionNumber { get; set; }
            public DateTime CreatedAt { get; set; }
            public string MinecraftVersion { get; set; } = "";
            public List<string> IncludedFiles { get; set; } = new();
        }

        public int GetNextPresetVersion()
        {
            int maxVersion = 0;
            foreach (var preset in GetPresets())
            {
                if (preset.VersionNumber > maxVersion)
                {
                    maxVersion = preset.VersionNumber;
                }
            }
            return maxVersion + 1;
        }

        public string BuildPresetName(int versionNumber) => $"Revision {versionNumber:D3}";

        public async Task<PresetMetadata> SavePresetAsync(string gameFolder, string presetName, string mcVersion)
        {
            string targetDir = Path.Combine(_presetsFolder, presetName);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
            Directory.CreateDirectory(targetDir);

            var filesToCopy = new[] { "options.txt", "servers.dat", "hotbar.nbt" };
            var dirsToCopy = new[] { "config", "shaderpacks", "resourcepacks" };

            var included = new List<string>();

            return await Task.Run(() =>
            {
                foreach (var file in filesToCopy)
                {
                    string source = Path.Combine(gameFolder, file);
                    if (File.Exists(source))
                    {
                        File.Copy(source, Path.Combine(targetDir, file), true);
                        included.Add(file);
                    }
                }

                foreach (var dir in dirsToCopy)
                {
                    string source = Path.Combine(gameFolder, dir);
                    if (Directory.Exists(source))
                    {
                        CopyDirectory(source, Path.Combine(targetDir, dir));
                        included.Add(dir);
                    }
                }

                int versionNumber = ExtractVersionNumber(presetName);
                var metadata = new PresetMetadata
                {
                    Name = presetName,
                    VersionNumber = versionNumber,
                    CreatedAt = DateTime.Now,
                    MinecraftVersion = mcVersion,
                    IncludedFiles = included
                };

                File.WriteAllText(Path.Combine(targetDir, "metadata.json"), 
                    JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

                return metadata;
            });
        }

        public List<PresetMetadata> GetPresets()
        {
            var result = new List<PresetMetadata>();
            if (!Directory.Exists(_presetsFolder)) return result;

            foreach (var dir in Directory.GetDirectories(_presetsFolder))
            {
                string metaPath = Path.Combine(dir, "metadata.json");
                if (File.Exists(metaPath))
                {
                    try
                    {
                        var meta = JsonSerializer.Deserialize<PresetMetadata>(File.ReadAllText(metaPath));
                        if (meta != null) result.Add(meta);
                    }
                    catch { }
                }
            }
            return result.OrderByDescending(p => p.CreatedAt).ToList();
        }

        public async Task ApplyPresetAsync(string gameFolder, string presetName, bool copyControls, bool copyGraphics, bool copyMods, bool copyOthers)
        {
            string sourceDir = Path.Combine(_presetsFolder, presetName);
            if (!Directory.Exists(sourceDir)) throw new DirectoryNotFoundException("Preset not found.");

            await Task.Run(() =>
            {
                // Backup current critical files before overwriting
                string backupDir = Path.Combine(gameFolder, "backups", "pre-preset-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(backupDir);

                if (copyGraphics || copyControls)
                {
                    string optFile = Path.Combine(gameFolder, "options.txt");
                    if (File.Exists(optFile)) File.Copy(optFile, Path.Combine(backupDir, "options.txt"), true);

                    if (copyControls) ApplyOptionsFilter(sourceDir, gameFolder, "key_");
                    if (copyGraphics) ApplyOptionsFilter(sourceDir, gameFolder, "graphics", "render", "gamma", "fov", "guiScale");
                    
                    if (!copyControls && !copyGraphics) // Full copy if both true and no filter logic needed? 
                                                       // Actually let's just copy the whole file if both are selected for simplicity, 
                                                       // or use specific logic.
                    {
                        // If both true, we just copy the whole file
                        File.Copy(Path.Combine(sourceDir, "options.txt"), Path.Combine(gameFolder, "options.txt"), true);
                    }
                }

                if (copyMods)
                {
                    string cfgDir = Path.Combine(gameFolder, "config");
                    if (Directory.Exists(cfgDir)) 
                    {
                        // Move current config to backup
                        CopyDirectory(cfgDir, Path.Combine(backupDir, "config"));
                    }
                    
                    string srcCfg = Path.Combine(sourceDir, "config");
                    if (Directory.Exists(srcCfg)) CopyDirectory(srcCfg, cfgDir);
                }

                if (copyOthers)
                {
                    string[] others = { "servers.dat", "hotbar.nbt", "shaderpacks", "resourcepacks" };
                    foreach (var item in others)
                    {
                        string src = Path.Combine(sourceDir, item);
                        string dst = Path.Combine(gameFolder, item);
                        if (File.Exists(src)) File.Copy(src, dst, true);
                        else if (Directory.Exists(src)) CopyDirectory(src, dst);
                    }
                }
            });
        }

        private void ApplyOptionsFilter(string srcDir, string dstDir, params string[] prefixes)
        {
            string srcFile = Path.Combine(srcDir, "options.txt");
            string dstFile = Path.Combine(dstDir, "options.txt");

            if (!File.Exists(srcFile)) return;
            if (!File.Exists(dstFile))
            {
                File.Copy(srcFile, dstFile);
                return;
            }

            var srcLines = File.ReadAllLines(srcFile);
            var dstLines = File.ReadAllLines(dstFile).ToList();

            foreach (var line in srcLines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains(":")) continue;
                string key = line.Split(':')[0];
                
                if (prefixes.Any(p => key.StartsWith(p)))
                {
                    int existingIdx = dstLines.FindIndex(l => l.StartsWith(key + ":"));
                    if (existingIdx >= 0) dstLines[existingIdx] = line;
                    else dstLines.Add(line);
                }
            }

            File.WriteAllLines(dstFile, dstLines);
        }

        private void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src))
                CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        public void DeletePreset(string name)
        {
            string path = Path.Combine(_presetsFolder, name);
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        private static int ExtractVersionNumber(string presetName)
        {
            string[] parts = presetName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && int.TryParse(parts[^1], out int parsed))
            {
                return parsed;
            }

            return 0;
        }
    }
}
