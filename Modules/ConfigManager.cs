using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace NebulaLauncher.Modules
{
    public class ConfigManager
    {
        private readonly string _gamePath;

        public ConfigManager(string gamePath) => _gamePath = gamePath;

        // --- options.txt de Minecraft ---
        public async Task ApplyPerformancePreset(string mode)
        {
            string optionsPath = Path.Combine(_gamePath, "options.txt");
            if (!File.Exists(optionsPath)) return;

            var settings = new Dictionary<string, string>();
            foreach (var line in await File.ReadAllLinesAsync(optionsPath))
            {
                var split = line.Split(':');
                if (split.Length == 2) settings[split[0]] = split[1].Trim();
            }

            if (mode == "Papa")
            {
                settings["renderDistance"] = "4";
                settings["enableClouds"] = "false";
                settings["graphicsMode"] = "0"; // Fast
                settings["particles"] = "2"; // Minimal
                settings["ao"] = "0"; // Smooth lighting Off
            }
            else if (mode == "Ultra")
            {
                settings["renderDistance"] = "16";
                settings["graphicsMode"] = "2"; // Fabulous!
                settings["enableClouds"] = "true";
                settings["particles"] = "0"; // All
                settings["ao"] = "2"; // Maximum
            }

            var output = new List<string>();
            foreach (var kvp in settings) output.Add($"{kvp.Key}:{kvp.Value}");
            await File.WriteAllLinesAsync(optionsPath, output);
        }

        // --- Mod Configs (.toml) ---
        public async Task UpdateTomlConfig(string configName, string key, object value)
        {
            try
            {
                string path = Path.Combine(_gamePath, "config", configName);
                if (!File.Exists(path)) return;

                string content = await File.ReadAllTextAsync(path);
                var model = Toml.ToModel(content);
                model[key] = value;

                string newContent = Toml.FromModel(model);
                await File.WriteAllTextAsync(path, newContent);
            }
            catch (IOException) { /* Archivo en uso por el juego */ }
        }
        public async Task UpdateSplashText(string text)
        {
            try
            {
                string textDir = Path.Combine(_gamePath, "texts");
                Directory.CreateDirectory(textDir);
                await File.WriteAllTextAsync(Path.Combine(textDir, "splashes.txt"), text);
            }
            catch { }
        }
    }
}
