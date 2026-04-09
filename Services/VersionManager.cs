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
        /// Reads the current application version from the binary metadata.
        /// This is the SINGLE SOURCE OF TRUTH for the installed and running binary.
        /// </summary>
        public static string GetCurrentVersion()
        {
            try
            {
                var fileVersion = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileVersionInfo.ProductVersion;
                if (string.IsNullOrEmpty(fileVersion))
                {
                    var assemblyV = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    return assemblyV != null ? $"{assemblyV.Major}.{assemblyV.Minor}.{assemblyV.Build}" : "1.0.0";
                }

                // Strip SourceLink metadata if present (e.g. 2.6.1+c8f151d -> 2.6.1)
                if (fileVersion.Contains("+"))
                    fileVersion = fileVersion.Split('+')[0];

                return CleanVersion(fileVersion);
            }
            catch { return "1.0.0"; }
        }

        /// <summary>
        /// Parses and cleans a version string, removing prefixes like 'v', suffixes, and extra whitespace.
        /// Handles cases like "v2.6.1-beta", "2.6.1+hash", etc.
        /// </summary>
        public static string CleanVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return "0.0.0";
            
            // 1. Remove common prefixes and whitespace
            string clean = version.Trim().ToLower();
            if (clean.StartsWith("v")) clean = clean.Substring(1);
            
            // 2. Split by common metadata separators (+, -)
            clean = clean.Split(new char[] { '+', '-' })[0];

            // 3. Extract exactly X.Y.Z using regex
            var match = Regex.Match(clean, @"^(\d+)\.(\d+)\.(\d+)");
            if (match.Success)
            {
                return $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
            }

            // Fallback: try to just find 3 numbers
            var looseMatch = Regex.Match(clean, @"(\d+)\.(\d+)\.(\d+)");
            return looseMatch.Success ? 
                $"{looseMatch.Groups[1].Value}.{looseMatch.Groups[2].Value}.{looseMatch.Groups[3].Value}" : 
                "0.0.0";
        }

        /// <summary>
        /// Performs a strict semantic comparison (Remote > Local).
        /// Correcty handles 2.6.9 vs 2.6.10 by using System.Version objects.
        /// </summary>
        public static bool IsNewer(string local, string remote)
        {
            string cleanLocal = CleanVersion(local);
            string cleanRemote = CleanVersion(remote);

            if (Version.TryParse(cleanLocal, out var l) && 
                Version.TryParse(cleanRemote, out var r))
            {
                return r > l;
            }
            return false;
        }

        /// <summary>
        /// Verifies if a specific binary file matches the target version.
        /// Used for pre-flight checks before publication.
        /// </summary>
        public static bool ValidateBinaryVersion(string path, string targetVersion)
        {
            if (!File.Exists(path)) return false;
            try
            {
                var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                string binVersion = CleanVersion(info.ProductVersion ?? info.FileVersion ?? "");
                return binVersion == CleanVersion(targetVersion);
            }
            catch { return false; }
        }

        /// <summary>
        /// Unit tests for versioning logic. Logs results to console/diagnostics.
        /// </summary>
        public static bool RunSelfTests(Action<string> log)
        {
            var tests = new (string local, string remote, bool expected)[]
            {
                ("2.6.1", "2.6.1", false),
                ("2.6.1", "2.6.2", true),
                ("2.6.9", "2.6.10", true),
                ("2.6.10", "2.6.9", false),
                ("2.6.1", "v2.6.1", false),
                ("2.6.1", "v2.6.5-beta", true),
                ("2.6.1+hash", "2.6.1", false),
                ("1.0.0", "2.0.0", true)
            };

            int failed = 0;
            foreach (var t in tests)
            {
                bool result = IsNewer(t.local, t.remote);
                if (result != t.expected)
                {
                    log?.Invoke($"❌ TEST FAILED: {t.local} vs {t.remote} | Got: {result}, Expected: {t.expected}");
                    failed++;
                }
            }

            if (failed == 0) log?.Invoke("✅ All VersionManager self-tests passed.");
            return failed == 0;
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
