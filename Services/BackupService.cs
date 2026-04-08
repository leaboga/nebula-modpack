using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace NebulaLauncher.Services
{
    public class BackupService
    {
        private readonly string _gameFolder;
        private readonly string _backupsFolder;

        public BackupService(string gameFolder)
        {
            _gameFolder   = gameFolder;
            _backupsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NebulaLauncher", "backups");
            Directory.CreateDirectory(_backupsFolder);
        }

        public string BackupsFolder => _backupsFolder;

        /// <summary>Creates a timestamped zip backup of saves + world data.</summary>
        public async Task<string> CreateBackupAsync(Action<string>? onLog = null)
        {
            return await Task.Run(() => PerformBackup(new[] { "saves", "screenshots", "config", "options.txt", "servers.dat" }, "backup", onLog));
        }

        /// <summary>Fast backup for configs only (runs before game launch).</summary>
        public async Task CreateQuickConfigBackupAsync()
        {
            await Task.Run(() => PerformBackup(new[] { "config", "options.txt", "servers.dat" }, "quick-config", null));
            CleanupOldBackups("quick-config-*.zip", 5); // Keep last 5 quick backups
        }

        private string PerformBackup(string[] targets, string prefix, Action<string>? onLog)
        {
            string stamp   = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string zipPath = Path.Combine(_backupsFolder, $"nebula-{prefix}-{stamp}.zip");

            string tempDir = Path.Combine(Path.GetTempPath(), "nebula-" + prefix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            foreach (var target in targets)
            {
                string source = Path.Combine(_gameFolder, target);
                try
                {
                    if (File.Exists(source))
                        File.Copy(source, Path.Combine(tempDir, Path.GetFileName(source)), true);
                    else if (Directory.Exists(source))
                        CopyDirectory(source, Path.Combine(tempDir, Path.GetFileName(source)));
                }
                catch { }
            }

            onLog?.Invoke($"📦 Comprimiendo {prefix}...");
            ZipFile.CreateFromDirectory(tempDir, zipPath);
            try { Directory.Delete(tempDir, true); } catch { }
            
            onLog?.Invoke($"✅ Backup {prefix} listo.");
            return zipPath;
        }

        private void CleanupOldBackups(string pattern, int keepCount)
        {
            try
            {
                var files = new DirectoryInfo(_backupsFolder).GetFiles(pattern);
                if (files.Length <= keepCount) return;
                
                Array.Sort(files, (a, b) => b.CreationTime.CompareTo(a.CreationTime));
                for (int i = keepCount; i < files.Length; i++) files[i].Delete();
            }
            catch { }
        }

        public BackupEntry[] GetBackupList()
        {
            if (!Directory.Exists(_backupsFolder)) return Array.Empty<BackupEntry>();
            var files = Directory.GetFiles(_backupsFolder, "nebula-backup-*.zip");
            var result = new System.Collections.Generic.List<BackupEntry>();
            foreach (var f in files)
            {
                var info = new FileInfo(f);
                result.Add(new BackupEntry
                {
                    FileName  = info.Name,
                    FullPath  = f,
                    Created   = info.CreationTime,
                    SizeMB    = info.Length / (1024.0 * 1024.0)
                });
            }
            result.Sort((a, b) => b.Created.CompareTo(a.Created));
            return result.ToArray();
        }

        public void DeleteBackup(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        public async Task CopyToCloudAsync(string zipPath, string cloudPath, Action<string>? onLog = null)
        {
            if (string.IsNullOrEmpty(cloudPath) || !Directory.Exists(cloudPath)) return;
            try
            {
                onLog?.Invoke("☁️ Sincronizando con la nube...");
                string dest = Path.Combine(cloudPath, Path.GetFileName(zipPath));
                await Task.Run(() => File.Copy(zipPath, dest, true));
                onLog?.Invoke("✅ Sincronización completa.");
            }
            catch (Exception ex) { onLog?.Invoke($"⚠ Error en nube: {ex.Message}"); }
        }

        private void CopyDirectory(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
                try { File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true); } catch { }
            foreach (var d in Directory.GetDirectories(src))
                CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));
        }
    }

    public class BackupEntry
    {
        public string   FileName { get; set; } = "";
        public string   FullPath { get; set; } = "";
        public DateTime Created  { get; set; }
        public double   SizeMB   { get; set; }
        public string   Label    => $"{Created:dd/MM/yyyy HH:mm}  —  {SizeMB:F1} MB";
    }
}
