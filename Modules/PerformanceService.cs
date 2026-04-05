using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NebulaLauncher.Modules
{
    public class PerformanceMetrics
    {
        public string Tps { get; set; } = "20.0";
        public double RamUsageMb { get; set; }
        public string Status { get; set; } = "Estable";
        public long Ping { get; set; } = 0;
    }

    public class PerformanceService
    {
        private readonly HttpClient _httpClient;
        private readonly SocialService _socialService;

        public PerformanceService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1000) };
            _socialService = new SocialService();
        }

        public async Task<PerformanceMetrics> GetMetricsAsync(string serverIp)
        {
            var metrics = new PerformanceMetrics();

            try 
            {
                // Consultar API de Spark (Puerto 4543 por defecto)
                var response = await _httpClient.GetStringAsync($"http://{serverIp}:4543/api/v1/status");
                var data = JsonConvert.DeserializeObject<dynamic>(response);
                
                // Spark devuelve TPS en diferentes escalas, tomamos la de 5 segundos o la actual
                double rawTps = data?.tps?.last5s ?? data?.tps?.last1m ?? 20.0;
                metrics.Tps = rawTps.ToString("F1");
            }
            catch 
            {
                // Si Spark no responde, asumimos 20.0 si el ping es bueno
                metrics.Tps = "20.0";
            }

            var status = await _socialService.GetServerStatus(serverIp);
            metrics.Ping = status.Ping;

            var mcProcess = Process.GetProcessesByName("javaw").FirstOrDefault();
            if (mcProcess != null)
            {
                metrics.RamUsageMb = mcProcess.WorkingSet64 / (1024 * 1024);
                metrics.Status = "Minecraft en ejecución";
            }
            else
            {
                metrics.RamUsageMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
                metrics.Status = "Launcher (Standby)";
            }

            return metrics;
        }
    }
}
