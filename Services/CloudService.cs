using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using KrakenLauncher;

namespace KrakenLauncher.Services
{
    public class CloudService
    {
        private static CloudService? _instance;
        public static CloudService Instance => _instance ??= new CloudService();

        private CloudService() { }

        public async Task SyncToCloud(UserSession session, string cloudFolder)
        {
            if (string.IsNullOrEmpty(cloudFolder) || !Directory.Exists(cloudFolder))
                return;

            try
            {
                string syncData = JsonConvert.SerializeObject(session, Formatting.Indented);
                string filePath = Path.Combine(cloudFolder, "nebula_sync.json");
                await File.WriteAllTextAsync(filePath, syncData);
            }
            catch (Exception ex)
            {
                throw new Exception("Fallo al sincronizar con la nube: " + ex.Message);
            }
        }

        public async Task<UserSession?> PullFromCloud(string cloudFolder)
        {
            if (string.IsNullOrEmpty(cloudFolder)) return null;

            string filePath = Path.Combine(cloudFolder, "nebula_sync.json");
            if (!File.Exists(filePath)) return null;

            try
            {
                string content = await File.ReadAllTextAsync(filePath);
                return JsonConvert.DeserializeObject<UserSession>(content);
            }
            catch { return null; }
        }
    }
}
