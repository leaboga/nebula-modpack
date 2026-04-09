using System;
using System.IO;

namespace NebulaLauncher.Services
{
    public static class PathService
    {
        public static readonly string AppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NebulaLauncher");
        public static readonly string SessionFile = Path.Combine(AppFolder, "session.json");
        public static readonly string LogFile = Path.Combine(AppFolder, "launcher.log");
        public static readonly string DiscoveryStateFile = Path.Combine(AppFolder, "discovery-state.json");
        public static readonly string InstancesFolder = Path.Combine(AppFolder, "instances");
        public static readonly string CacheFolder = Path.Combine(AppFolder, "cache");
        public static readonly string ServersFolder = Path.Combine(AppFolder, "servers");

        public static void Initialize()
        {
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
