using System;
using System.IO;

namespace KrakenLauncher.Services
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KrakenLauncher", "kraken_debug.log");

        static Logger()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                
                // Archivar logs viejos si superan 1MB
                if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 1024 * 1024)
                {
                    File.Move(LogFile, LogFile + ".old", true);
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}\n");
            }
            catch { }
        }

        public static void LogError(string message, Exception? ex = null)
        {
            try
            {
                File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}\n");
                if (ex != null)
                {
                    File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [EXCEPTION] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n");
                }
            }
            catch { }
        }
    }
}
