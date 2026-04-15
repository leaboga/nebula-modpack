using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KrakenLauncher.Services
{
    public class CrashReporterService
    {
        private readonly string _gameFolder;
        private readonly string _webhookUrl;

        public CrashReporterService(string gameFolder, string webhookUrl = "")
        {
            _gameFolder = gameFolder;
            _webhookUrl = webhookUrl;
        }

        public class CrashAnalysis
        {
            public string FileName { get; set; } = "";
            public string FullLog { get; set; } = "";
            public string DetectedError { get; set; } = "Error desconocido";
            public string UserSolution { get; set; } = "Revisa los logs para más detalle o intenta reinstalar el modpack.";
            public bool IsRecoverable { get; set; } = false;
        }

        public CrashAnalysis? AnalyzeLastCrash(DateTime sinceTime)
        {
            string crashDir = Path.Combine(_gameFolder, "crash-reports");
            if (!Directory.Exists(crashDir)) return null;

            FileInfo? latest = null;
            foreach (var file in Directory.GetFiles(crashDir, "crash-*.txt"))
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime > sinceTime)
                {
                    if (latest == null || info.LastWriteTime > latest.LastWriteTime) latest = info;
                }
            }

            if (latest == null) return null;

            try
            {
                string content = File.ReadAllText(latest.FullName, Encoding.UTF8);
                var analysis = new CrashAnalysis { FileName = latest.Name, FullLog = content };

                // LOGICA DE ANALISIS SEMANTICO
                if (content.Contains("java.lang.OutOfMemoryError"))
                {
                    analysis.DetectedError = "Falta de Memoria RAM";
                    analysis.UserSolution = "Aumenta la RAM asignada en los ajustes del launcher (se recomienda al menos 6GB o 8GB).";
                }
                else if (content.Contains("java.lang.UnsupportedClassVersionError"))
                {
                    analysis.DetectedError = "Versión de Java incorrecta";
                    analysis.UserSolution = "El juego requiere una versión más reciente de Java. El launcher debería descargarla automáticamente, intenta reiniciar.";
                }
                else if (content.Contains("Missing or unsupported mandatory dependencies"))
                {
                    analysis.DetectedError = "Faltan dependencias de Mods";
                    analysis.UserSolution = "Algunos mods requieren otros que no están instalados. Usa el botón de 'Reparar' para restaurar el modpack completo.";
                    analysis.IsRecoverable = true;
                }
                else if (content.Contains("Incompatible mod setfound"))
                {
                    analysis.DetectedError = "Mods Incompatibles";
                    analysis.UserSolution = "Has añadido mods manualmente que causan conflicto. Intenta desactivarlos en el Gestor de Mods.";
                }

                return analysis;
            }
            catch { return null; }
        }

        /// <summary>
        /// Checks if a new crash report appeared in crash-reports/ since the given time.
        /// Returns the crash summary string, or null if no crash happened.
        /// </summary>
        public string? CheckForCrash(DateTime sinceTime)
        {
            var analysis = AnalyzeLastCrash(sinceTime);
            if (analysis == null) return null;

            var sb = new StringBuilder();
            sb.AppendLine($"📄 {analysis.FileName}");
            sb.AppendLine($"⚠️ **{analysis.DetectedError}**");
            sb.AppendLine($"💡 {analysis.UserSolution}");
            sb.AppendLine("\n```");
            var lines = analysis.FullLog.Split('\n');
            int take = Math.Min(30, lines.Length);
            for (int i = 0; i < take; i++) sb.AppendLine(lines[i].TrimEnd());
            sb.AppendLine("```");
            return sb.ToString();
        }

        /// <summary>Sends crash report to a Discord webhook.</summary>
        public async Task<bool> ReportToDiscordAsync(string crashSummary, string playerName)
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl)) return false;
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                string payload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    username   = "Nebula Crash Bot",
                    avatar_url = "https://cdn.pixabay.com/photo/2017/08/10/05/18/eye-2618684_1280.jpg",
                    content    = $"💥 **Crash detectado** para `{playerName}`\n{crashSummary}"
                });
                var response = await http.PostAsync(
                    _webhookUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        /// <summary>Opens the crash-reports folder in Explorer.</summary>
        public void OpenCrashFolder()
        {
            string dir = Path.Combine(_gameFolder, "crash-reports");
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
    }

}
