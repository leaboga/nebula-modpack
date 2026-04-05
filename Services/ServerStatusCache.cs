using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NebulaLauncher.Modules;

namespace NebulaLauncher.Services
{
    /// <summary>
    /// Caches the last known server status to disk so the UI always has something to show,
    /// even when the server is unreachable.
    /// </summary>
    public class ServerStatusCache
    {
        private readonly string _cachePath;

        public ServerStatusCache()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NebulaLauncher");
            Directory.CreateDirectory(dir);
            _cachePath = Path.Combine(dir, "server_cache.json");
        }

        public CachedServerStatus Load()
        {
            try
            {
                if (File.Exists(_cachePath))
                    return JsonConvert.DeserializeObject<CachedServerStatus>(
                        File.ReadAllText(_cachePath)) ?? new CachedServerStatus();
            }
            catch { }
            return new CachedServerStatus();
        }

        public void Save(ServerInfo status)
        {
            try
            {
                var cached = new CachedServerStatus
                {
                    Status   = status,
                    LastSeen = DateTime.Now
                };
                File.WriteAllText(_cachePath, JsonConvert.SerializeObject(cached, Formatting.Indented));
            }
            catch { }
        }

        /// <summary>
        /// Returns the cache's ServerInfo if the server is currently offline,
        /// enriched with a flag indicating it's stale data.
        /// </summary>
        public string GetLastSeenLabel(DateTime lastSeen)
        {
            var diff = DateTime.Now - lastSeen;
            if (diff.TotalMinutes < 2)   return "hace un momento";
            if (diff.TotalMinutes < 60)  return $"hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours   < 24)  return $"hace {(int)diff.TotalHours} h";
            return $"hace {(int)diff.TotalDays} días";
        }
    }

    public class CachedServerStatus
    {
        public ServerInfo Status   { get; set; } = new ServerInfo();
        public DateTime   LastSeen { get; set; } = DateTime.MinValue;
        public bool       HasData  => LastSeen != DateTime.MinValue;
    }
}
