using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NebulaLauncher.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new object();
        private static readonly Queue<string> _recentEntries = new Queue<string>();
        private const int MaxRecentEntries = 250;

        public static event Action<string>? OnLogReceived;

        public static void Log(string message, string tag = "CORE")
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tag}] {message}";
            OnLogReceived?.Invoke(logLine);
            
            #if DEBUG
            System.Diagnostics.Debug.WriteLine(logLine);
            #endif

            try
            {
                lock (_lock)
                {
                    _recentEntries.Enqueue(logLine);
                    while (_recentEntries.Count > MaxRecentEntries)
                        _recentEntries.Dequeue();

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

        public static IReadOnlyList<string> GetRecentEntries(int maxEntries = 50)
        {
            lock (_lock)
            {
                return _recentEntries.Skip(Math.Max(0, _recentEntries.Count - maxEntries)).ToList();
            }
        }

        public static void ClearLogFile()
        {
            lock (_lock)
            {
                _recentEntries.Clear();
                File.WriteAllText(PathService.LogFile, string.Empty, Encoding.UTF8);
            }
        }
    }
}
