using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaLauncher.Services
{
    public class ModrinthMod
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconUrl { get; set; } = "";
        public string ProjectId { get; set; } = "";
        public string Author { get; set; } = "";
        public int Downloads { get; set; }
        public List<string> Categories { get; set; } = new();
        public string DateModified { get; set; } = "";
        public string DisplayDownloads => (Downloads >= 1000000) ? (Downloads / 1000000.0).ToString("0.#") + "M" : (Downloads >= 1000) ? (Downloads / 1000.0).ToString("0.#") + "K" : Downloads.ToString();
        public bool IsFavorite { get; set; }
        public bool IsRecent { get; set; }
        public string FavoriteGlyph => IsFavorite ? "★" : "☆";
        public string CategoryLabel => Categories.Count > 0 ? string.Join(" · ", Categories.Take(2)) : "General";
    }

    public class ModrinthService
    {
        private static readonly HttpClient _client = new() { BaseAddress = new Uri("https://api.modrinth.com/v2/") };

        public ModrinthService()
        {
            _client.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher/1.5 (leandro@nebula.com)");
        }

        public async Task<List<ModrinthMod>> SearchMods(string query, string version = "1.20.1", string loader = "fabric")
        {
            try
            {
                string facets = $"[[\"versions:{version}\"],[\"categories:{loader}\"],[\"project_type:mod\"]]";
                var response = await _client.GetAsync($"search?query={Uri.EscapeDataString(query)}&facets={Uri.EscapeDataString(facets)}&limit=15");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                dynamic? result = JsonConvert.DeserializeObject(content);
                var mods = new List<ModrinthMod>();

                if (result?.hits != null)
                {
                    foreach (var hit in result.hits)
                    {
                        mods.Add(new ModrinthMod
                        {
                            Title = hit.title,
                            Description = hit.description,
                            IconUrl = hit.icon_url,
                            ProjectId = hit.project_id,
                            Author = hit.author,
                            Downloads = hit.downloads ?? 0,
                            Categories = hit.categories?.ToObject<List<string>>() ?? new List<string>(),
                            DateModified = hit.date_modified
                        });
                    }
                }
                return mods;
            }
            catch { return new List<ModrinthMod>(); }
        }

        public async Task<List<ModrinthMod>> SearchModpacks(string query, string? version = null, string? loader = null, string? category = null)
        {
            try
            {
                var facetGroups = new List<string> { "[\"project_type:modpack\"]" };
                if (!string.IsNullOrEmpty(version)) facetGroups.Add($"[\"versions:{version}\"]");
                if (!string.IsNullOrEmpty(loader)) facetGroups.Add($"[\"categories:{loader}\"]");
                if (!string.IsNullOrEmpty(category) && category != "all") facetGroups.Add($"[\"categories:{category}\"]");

                string facets = $"[{string.Join(",", facetGroups)}]";
                var response = await _client.GetAsync($"search?query={Uri.EscapeDataString(query)}&facets={Uri.EscapeDataString(facets)}&limit=20");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                dynamic? result = JsonConvert.DeserializeObject(content);
                var mods = new List<ModrinthMod>();

                if (result?.hits != null)
                {
                    foreach (var hit in result.hits)
                    {
                        mods.Add(new ModrinthMod
                        {
                            Title = hit.title,
                            Description = hit.description,
                            IconUrl = hit.icon_url,
                            ProjectId = hit.project_id,
                            Author = hit.author,
                            Downloads = hit.downloads ?? 0,
                            Categories = hit.categories?.ToObject<List<string>>() ?? new List<string>(),
                            DateModified = hit.date_modified
                        });
                    }
                }
                return mods;
            }
            catch (Exception ex) 
            { 
                Console.WriteLine("Error en búsqueda de modpacks: " + ex.Message);
                return new List<ModrinthMod>(); 
            }
        }

        public async Task<string?> GetLatestVersionDownloadUrl(string projectId)
        {
            try
            {
                var response = await _client.GetAsync($"project/{projectId}/version");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                dynamic? versions = JsonConvert.DeserializeObject(content);

                if (versions != null && versions.Count > 0)
                {
                    var latest = versions[0];
                    return latest.files[0].url;
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> DownloadMod(string projectId, string version, string loader, string modsFolder)
        {
            try
            {
                var response = await _client.GetAsync($"project/{projectId}/version");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                dynamic? versions = JsonConvert.DeserializeObject(content);

                if (versions == null) return false;

                foreach (var v in versions)
                {
                    // Check compatibility
                    bool verOk = false;
                    foreach(var gameVer in v.game_versions) if(gameVer == version) verOk = true;
                    
                    bool loaderOk = false;
                    foreach(var l in v.loaders) if(l == loader) loaderOk = true;

                    if (verOk && loaderOk)
                    {
                        var file = v.files[0];
                        string downloadUrl = file.url;
                        string fileName = file.filename;

                        byte[] data = await _client.GetByteArrayAsync(downloadUrl);
                        Directory.CreateDirectory(modsFolder);
                        await File.WriteAllBytesAsync(Path.Combine(modsFolder, fileName), data);
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }
    }
}
