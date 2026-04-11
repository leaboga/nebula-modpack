using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaLauncher.Services
{
    public class ModrinthDependency
    {
        public string ProjectId { get; set; } = "";
        public string DependencyType { get; set; } = ""; // required, optional, etc
    }

    public class DependencyService
    {
        private static readonly HttpClient _client = new() { BaseAddress = new Uri("https://api.modrinth.com/v2/") };
        
        static DependencyService()
        {
            if (!_client.DefaultRequestHeaders.Contains("User-Agent"))
                _client.DefaultRequestHeaders.Add("User-Agent", "NebulaLauncher/1.5 (leandro@nebula.com)");
        }

        public async Task<List<string>> GetRequiredDependencies(string projectId, string version, string loader)
        {
            try
            {
                // 1. Get project versions
                var response = await _client.GetAsync($"project/{projectId}/version");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                dynamic? versions = JsonConvert.DeserializeObject(content);

                if (versions == null) return new List<string>();

                // 2. Find best matching version
                foreach (var v in versions)
                {
                    bool versionMatch = false;
                    foreach (var gv in v.game_versions) if (gv == version) versionMatch = true;
                    
                    bool loaderMatch = false;
                    foreach (var l in v.loaders) if (l == loader) loaderMatch = true;

                    if (versionMatch && loaderMatch)
                    {
                        var dependencies = new List<string>();
                        if (v.dependencies != null)
                        {
                            foreach (var dep in v.dependencies)
                            {
                                if (dep.dependency_type == "required")
                                {
                                    dependencies.Add((string)dep.project_id);
                                }
                            }
                        }
                        return dependencies;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DependencyService] Error: {ex.Message}");
            }
            return new List<string>();
        }
    }
}
