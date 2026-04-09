using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NebulaLauncher.Services
{
    public enum VersionSegment
    {
        Major,
        Minor,
        Patch
    }

    public static class VersionManager
    {
        /// <summary>
        /// Increments a version string (e.g., "2.6.0") based on the specified segment.
        /// </summary>
        public static string Increment(string currentVersion, VersionSegment segment)
        {
            if (!Version.TryParse(currentVersion, out var v))
                return currentVersion;

            int major = v.Major;
            int minor = v.Minor;
            int patch = Math.Max(0, v.Build); // System.Version uses Build for the 3rd component

            switch (segment)
            {
                case VersionSegment.Major:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case VersionSegment.Minor:
                    minor++;
                    patch = 0;
                    break;
                case VersionSegment.Patch:
                    patch++;
                    break;
            }

            return $"{major}.{minor}.{patch}";
        }

        /// <summary>
        /// Attempts to find and update the version in the .csproj file.
        /// This is intended for use during internal 'Publish' operations from the dev environment.
        /// </summary>
        public static bool UpdateProjectVersion(string projectRoot, string newVersion)
        {
            try
            {
                var csprojFiles = Directory.GetFiles(projectRoot, "*.csproj");
                if (csprojFiles.Length == 0) return false;

                string csprojPath = csprojFiles[0];
                string content = File.ReadAllText(csprojPath);

                // Update <Version> tag
                content = Regex.Replace(content, @"<Version>.*?</Version>", $"<Version>{newVersion}</Version>");
                
                // Update AssemblyVersion and FileVersion if they exist
                content = Regex.Replace(content, @"<AssemblyVersion>.*?</AssemblyVersion>", $"<AssemblyVersion>{newVersion}.0</AssemblyVersion>");
                content = Regex.Replace(content, @"<FileVersion>.*?</FileVersion>", $"<FileVersion>{newVersion}.0</FileVersion>");

                File.WriteAllText(csprojPath, content);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Updates the launcher-version.json file used by the update service.
        /// </summary>
        public static void UpdateVersionJson(string path, string newVersion, string downloadUrl, string notes)
        {
            try
            {
                var info = new
                {
                    version = newVersion,
                    downloadUrl = downloadUrl,
                    notes = notes
                };
                File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented));
            }
            catch { }
        }
    }
}
