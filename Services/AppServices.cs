using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace NebulaLauncher.Services
{
    public class ChangelogEntry
    {
        public string Version { get; set; } = "";
        public string Date    { get; set; } = "";
        public string Title   { get; set; } = "";
        public List<string> Changes { get; set; } = new();
        public string Type   { get; set; } = "update"; // update | fix | hotfix
    }

    public class ChangelogService
    {
        private const string Url = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/changelog.json?t=";
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

        public async Task<List<ChangelogEntry>> GetChangelogAsync()
        {
            try
            {
                string json = await _http.GetStringAsync(Url + DateTime.Now.Ticks);
                return JsonConvert.DeserializeObject<List<ChangelogEntry>>(json) ?? FallbackChangelog();
            }
            catch { return FallbackChangelog(); }
        }

        private static List<ChangelogEntry> FallbackChangelog() => new()
        {
            new ChangelogEntry
            {
                Version = "1.0.0",
                Date    = "Hoy",
                Title   = "Lanzamiento del modpack",
                Type    = "update",
                Changes = new List<string>
                {
                    "Pack inicial con NeoForge 1.21.1",
                    "Shaders SEUS PTGI incluidos",
                    "Performance presets configurados",
                    "Discord RPC integrado"
                }
            }
        };
    }

    public class SkinService
    {
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        /// <summary>Returns the face BitmapImage for a given Minecraft username (8x8 head crop).</summary>
        public async Task<BitmapImage?> GetSkinHeadAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            try
            {
                string url  = $"https://mc-heads.net/avatar/{username}/64";
                byte[] data = await _http.GetByteArrayAsync(url);
                using var ms = new System.IO.MemoryStream(data);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption  = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }
    }

    public class LauncherUpdateService
    {
        private const string VersionUrl = "https://raw.githubusercontent.com/leaboga/nebula-modpack/main/launcher-version.json";
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(6) };

        public async Task<UpdateInfo?> CheckForUpdateAsync(string currentVersion, Action<string, string> logDebug)
        {
            try
            {
                string url = VersionUrl + "?t=" + DateTime.Now.Ticks;
                string json = await _http.GetStringAsync(url);
                var info = JsonConvert.DeserializeObject<UpdateInfo>(json);
                
                if (info != null)
                {
                    logDebug?.Invoke(info.Version, currentVersion);
                    if (IsNewerVersion(info.Version, currentVersion))
                        return info;
                }
            }
            catch (Exception ex) { 
                Debug.WriteLine($"Update check error: {ex.Message}");
            }
            return null;
        }

        private static bool IsNewerVersion(string remote, string local)
        {
            if (Version.TryParse(remote, out var r) && Version.TryParse(local, out var l))
                return r > l;
            return false;
        }

        public async Task DownloadAndApplyUpdateAsync(UpdateInfo info, Action<string> log)
        {
            try
            {
                log($"⬇ Descargando v{info.Version}...");
                byte[] data = await _http.GetByteArrayAsync(info.DownloadUrl);
                
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName!;
                string newExe     = currentExe + ".tmp";
                string batFile    = Path.Combine(Path.GetTempPath(), "nebula_self_update.bat");

                if (File.Exists(newExe)) File.Delete(newExe);
                File.WriteAllBytes(newExe, data);

                // Script to replace the EXE while the launcher is closed
                string batContent = $@"
@echo off
title Nebula Updater
echo Esperando a que el launcher se cierre...
timeout /t 2 /nobreak > nul
:wait
tasklist /fi ""pid eq {Process.GetCurrentProcess().Id}"" | find "":"" > nul
if errorlevel 1 (
    timeout /t 1 /nobreak > nul
    goto wait
)
echo Reemplazando archivos...
del /f /q ""{currentExe}""
move /y ""{newExe}"" ""{currentExe}""
echo Iniciando nueva version...
start """" ""{currentExe}""
del ""%~f0""
";
                File.WriteAllText(batFile, batContent);
                
                log("🚀 Reiniciando para aplicar cambios...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batFile}\"",
                    CreateNoWindow = true,
                    UseShellExecute = true
                });
                
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                log($"❌ Error crítico de actualización: {ex.Message}");
                throw;
            }
        }
    }

    public class UpdateInfo
    {
        public string Version     { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string Notes       { get; set; } = "";
    }

    public class SessionHistoryService
    {
        private readonly string _filePath;

        public SessionHistoryService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appData, "NebulaLauncher", "session_history.json");
        }

        public SessionHistory Load()
        {
            try
            {
                if (File.Exists(_filePath))
                    return JsonConvert.DeserializeObject<SessionHistory>(File.ReadAllText(_filePath)) ?? new SessionHistory();
            }
            catch { }
            return new SessionHistory();
        }

        public void RecordSession(TimeSpan duration)
        {
            var history = Load();
            history.TotalMinutes  += (int)duration.TotalMinutes;
            history.SessionCount  += 1;
            history.LastPlayed     = DateTime.Now;
            history.Sessions.Add(new SessionRecord
            {
                Date     = DateTime.Now,
                Duration = (int)duration.TotalMinutes
            });
            // Keep only last 30 sessions
            if (history.Sessions.Count > 30) history.Sessions.RemoveAt(0);
            try { File.WriteAllText(_filePath, JsonConvert.SerializeObject(history, Formatting.Indented)); }
            catch { }
        }

        public string FormatTotalTime(int totalMinutes)
        {
            if (totalMinutes < 60)   return $"{totalMinutes}m";
            if (totalMinutes < 1440) return $"{totalMinutes / 60}h {totalMinutes % 60}m";
            return $"{totalMinutes / 1440}d {(totalMinutes % 1440) / 60}h";
        }
    }

    public class SessionHistory
    {
        public int TotalMinutes { get; set; }
        public int SessionCount { get; set; }
        public DateTime LastPlayed { get; set; }
        public List<SessionRecord> Sessions { get; set; } = new();
    }

    public class SessionRecord
    {
        public DateTime Date     { get; set; }
        public int      Duration { get; set; }  // minutes
    }
}
