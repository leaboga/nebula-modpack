using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace KrakenLauncher.Services
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

    public class SessionHistoryService
    {
        private readonly string _filePath;

        public SessionHistoryService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appData, "KrakenLauncher", "session_history.json");
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
