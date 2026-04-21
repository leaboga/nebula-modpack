using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace KrakenLauncher.Services
{
    public static class MandatoryFixesService
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private const string AeronauticsFix = "Java.loadClass('dev.eriksonn.aeronautics.service.AeroLevititeService').INSTANCE\r\n";

        private const string PackMeta = """
{
  "pack": {
    "pack_format": 48,
    "description": "Kraken mandatory Patchouli fixes for Minecraft 1.21.1"
  }
}
""";

        private const string BetterNetherBook = """
{
  "name": "item.betternether.guide_book",
  "landing_text": "Kraken Patchouli resource-pack compatibility fix for Minecraft 1.21.1.",
  "version": 1,
  "creative_tab": "betternether:betternether_tab",
  "use_resource_pack": true
}
""";

        private const string BetterEndBook = """
{
  "name": "item.betterend.guidebook",
  "landing_text": "Kraken Patchouli resource-pack compatibility fix for Minecraft 1.21.1.",
  "version": 1,
  "creative_tab": "betterend:betterend_tab",
  "use_resource_pack": true
}
""";

        public static void ApplyToKnownClientFolders(string? primaryGameFolder, Action<string>? log = null)
        {
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddIfValid(targets, primaryGameFolder);
            AddIfValid(targets, Path.Combine(PathService.AppFolder, "minecraft"));

            if (Directory.Exists(PathService.InstancesFolder))
            {
                foreach (var instance in Directory.GetDirectories(PathService.InstancesFolder))
                    AddIfValid(targets, instance);
            }

            foreach (var target in targets)
                ApplyToFolder(target, log);
        }

        public static void ApplyToFolder(string gameFolder, Action<string>? log = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(gameFolder)) return;
                Directory.CreateDirectory(gameFolder);

                WriteFile(gameFolder, Path.Combine("kubejs", "startup_scripts", "aeronautics_fix.js"), AeronauticsFix);

                string packRoot = Path.Combine("config", "openloader", "packs", "kraken_mandatory_patchouli_fixes");
                WriteFile(gameFolder, Path.Combine(packRoot, "pack.mcmeta"), PackMeta);
                WriteFile(gameFolder, Path.Combine(packRoot, "data", "betternether", "patchouli_books", "betternether_book", "book.json"), BetterNetherBook);
                WriteFile(gameFolder, Path.Combine(packRoot, "data", "betterend", "patchouli_books", "guidebook", "book.json"), BetterEndBook);

                log?.Invoke("Kraken mandatory fixes applied: Aeronautics KubeJS + Patchouli OpenLoader.");
            }
            catch (Exception ex)
            {
                log?.Invoke("Warning: could not apply Kraken mandatory fixes: " + ex.Message);
            }
        }

        private static void AddIfValid(HashSet<string> targets, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            targets.Add(Path.GetFullPath(path));
        }

        private static void WriteFile(string root, string relativePath, string content)
        {
            string fullPath = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content, Utf8NoBom);
        }
    }
}
