using System;
using System.IO;
using Newtonsoft.Json;

namespace NebulaLauncher.Services
{
    public sealed class UpdateDiagnosticsSnapshot
    {
        public string LocalVersion { get; set; } = "";
        public string RemoteVersion { get; set; } = "";
        public string AssetName { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string CurrentExePath { get; set; } = "";
        public string TargetExePath { get; set; } = "";
        public string Status { get; set; } = "idle";
        public string LastError { get; set; } = "";
        public bool Automatic { get; set; }
        public DateTime LastCheckedUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    }

    public static class UpdateDiagnosticsService
    {
        private static readonly object Sync = new object();

        public static UpdateDiagnosticsSnapshot Load()
        {
            lock (Sync)
            {
                try
                {
                    if (!File.Exists(PathService.UpdateStateFile))
                        return new UpdateDiagnosticsSnapshot();

                    return JsonConvert.DeserializeObject<UpdateDiagnosticsSnapshot>(File.ReadAllText(PathService.UpdateStateFile))
                           ?? new UpdateDiagnosticsSnapshot();
                }
                catch
                {
                    return new UpdateDiagnosticsSnapshot();
                }
            }
        }

        public static void Save(Action<UpdateDiagnosticsSnapshot> update)
        {
            lock (Sync)
            {
                PathService.Initialize();
                var snapshot = Load();
                update(snapshot);
                snapshot.LastUpdatedUtc = DateTime.UtcNow;
                File.WriteAllText(PathService.UpdateStateFile, JsonConvert.SerializeObject(snapshot, Formatting.Indented));
            }
        }

        public static void MarkCheck(string localVersion, string remoteVersion, string assetName, string downloadUrl, string currentExePath)
        {
            Save(snapshot =>
            {
                snapshot.LocalVersion = localVersion;
                snapshot.RemoteVersion = remoteVersion;
                snapshot.AssetName = assetName;
                snapshot.DownloadUrl = downloadUrl;
                snapshot.CurrentExePath = currentExePath;
                snapshot.Status = "detected";
                snapshot.LastError = string.Empty;
                snapshot.LastCheckedUtc = DateTime.UtcNow;
            });
        }

        public static void MarkNoUpdate(string localVersion, string remoteVersion)
        {
            Save(snapshot =>
            {
                snapshot.LocalVersion = localVersion;
                snapshot.RemoteVersion = remoteVersion;
                snapshot.Status = "no-update";
                snapshot.LastError = string.Empty;
                snapshot.LastCheckedUtc = DateTime.UtcNow;
            });
        }

        public static void MarkApplying(string targetExePath, bool automatic)
        {
            Save(snapshot =>
            {
                snapshot.TargetExePath = targetExePath;
                snapshot.Automatic = automatic;
                snapshot.Status = "applying";
                snapshot.LastError = string.Empty;
            });
        }

        public static void MarkRestartScheduled()
        {
            Save(snapshot =>
            {
                snapshot.Status = "restart-scheduled";
            });
        }

        public static void MarkFailure(string error)
        {
            Save(snapshot =>
            {
                snapshot.Status = "failed";
                snapshot.LastError = error;
            });
        }
    }
}
