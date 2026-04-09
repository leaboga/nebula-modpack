using System;
using System.IO;
using System.Text;

namespace NebulaLauncher.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new object();

        public static void Log(string message, string tag = "CORE")
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {message}";
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine(logLine);
            #endif

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(PathService.LogFile, logLine + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { /* Fail silently to avoid crashing the launcher due to log issues */ }
        }

        public static void Error(string message, Exception? ex = null)
        {
            string err = $"[ERROR] {message}";
            if (ex != null) err += $" | Exception: {ex.Message}";
            Log(err, "CRITICAL");
        }

        public static void Cleanup()
        {
            try
            {
                if (File.Exists(PathService.LogFile))
                {
                    var info = new FileInfo(PathService.LogFile);
                    if (info.Length > 1024 * 1024 * 5) // 5MB limit
                    {
                        File.WriteAllText(PathService.LogFile, "[LOG TRUNCATED - FILE EXCEEDED 5MB]" + Environment.NewLine);
                    }
                }
            }
            catch { }
        }
    }
}
