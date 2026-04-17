using System;
using System.IO;

namespace KrakenLauncher.Services
{
    public static class PathService
    {
        private static string _appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KrakenLauncher");
        public static string AppFolder => _appFolder;

        public static readonly string SessionFile = Path.Combine(AppFolder, "session.json");
        public static readonly string LogFile = Path.Combine(AppFolder, "launcher.log");
        public static readonly string UpdateStateFile = Path.Combine(AppFolder, "update-state.json");
        public static readonly string UpdaterLogFile = Path.Combine(AppFolder, "updater.log");
        public static readonly string DiscoveryStateFile = Path.Combine(AppFolder, "discovery-state.json");
        public static readonly string InstancesFolder = Path.Combine(AppFolder, "instances");
        public static readonly string CacheFolder = Path.Combine(AppFolder, "cache");
        public static readonly string ServersFolder = Path.Combine(AppFolder, "servers");

        public static void Initialize()
        {
            // Migración: Si existe la carpeta vieja de Nebula y no la nueva de Kraken, renombrar.
            string oldFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher");
            if (Directory.Exists(oldFolder) && !Directory.Exists(_appFolder))
            {
                try { Directory.Move(oldFolder, _appFolder); } catch { }
            }

            Directory.CreateDirectory(AppFolder);
            Directory.CreateDirectory(InstancesFolder);
            Directory.CreateDirectory(CacheFolder);
            Directory.CreateDirectory(ServersFolder);
        }

        public static string GetInstanceFolder(string profileId)
        {
            string path = Path.Combine(InstancesFolder, profileId);
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
