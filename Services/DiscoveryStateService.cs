using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NebulaLauncher.Services
{
    public class DiscoveryState
    {
        public List<string> FavoriteMods { get; set; } = new();
        public List<string> FavoriteModpacks { get; set; } = new();
        public List<string> RecentMods { get; set; } = new();
        public List<string> RecentModpacks { get; set; } = new();
    }

    public sealed class DiscoveryStateService
    {
        private static readonly Lazy<DiscoveryStateService> _instance = new(() => new DiscoveryStateService());
        public static DiscoveryStateService Instance => _instance.Value;

        private readonly object _lock = new();
        private DiscoveryState _state = new();

        private DiscoveryStateService()
        {
            Load();
        }

        public bool IsFavoriteMod(string projectId) => _state.FavoriteMods.Contains(projectId, StringComparer.OrdinalIgnoreCase);
        public bool IsFavoriteModpack(string projectId) => _state.FavoriteModpacks.Contains(projectId, StringComparer.OrdinalIgnoreCase);
        public bool IsRecentMod(string projectId) => _state.RecentMods.Contains(projectId, StringComparer.OrdinalIgnoreCase);
        public bool IsRecentModpack(string projectId) => _state.RecentModpacks.Contains(projectId, StringComparer.OrdinalIgnoreCase);

        public void ToggleFavoriteMod(string projectId) => Toggle(_state.FavoriteMods, projectId);
        public void ToggleFavoriteModpack(string projectId) => Toggle(_state.FavoriteModpacks, projectId);

        public void MarkRecentMod(string projectId) => MarkRecent(_state.RecentMods, projectId);
        public void MarkRecentModpack(string projectId) => MarkRecent(_state.RecentModpacks, projectId);

        private void Toggle(List<string> list, string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) return;

            lock (_lock)
            {
                int index = list.FindIndex(x => string.Equals(x, projectId, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) list.RemoveAt(index);
                else list.Insert(0, projectId);
                Save();
            }
        }

        private void MarkRecent(List<string> list, string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId)) return;

            lock (_lock)
            {
                list.RemoveAll(x => string.Equals(x, projectId, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, projectId);
                if (list.Count > 20) list.RemoveRange(20, list.Count - 20);
                Save();
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(PathService.DiscoveryStateFile))
                {
                    _state = JsonConvert.DeserializeObject<DiscoveryState>(File.ReadAllText(PathService.DiscoveryStateFile)) ?? new DiscoveryState();
                }
            }
            catch
            {
                _state = new DiscoveryState();
            }
        }

        private void Save()
        {
            try
            {
                Directory.CreateDirectory(PathService.AppFolder);
                File.WriteAllText(PathService.DiscoveryStateFile, JsonConvert.SerializeObject(_state, Formatting.Indented));
            }
            catch
            {
                // Keep discovery state best-effort only.
            }
        }
    }
}
