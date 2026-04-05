using System;
using System.Collections.Generic;

namespace NebulaLauncher
{
    public class UserSession
    {
        public string AuthMode               { get; set; } = "offline";
        public string Username               { get; set; } = "Viajero";
        public string CurrentProfileId       { get; set; } = "";
        public List<MinecraftProfile> Profiles { get; set; } = new();
        public bool   MinimizeToTray         { get; set; } = false;
        public bool   IsAdmin                { get; set; } = false;
        public string MsSession              { get; set; } = "";
        public string ServerIp               { get; set; } = "200.117.208.146";
        public string CrashWebhookUrl        { get; set; } = "";
        public string BlueMapPort            { get; set; } = "8100";
        public string BlueMapId              { get; set; } = "world";
        public string AccentColor            { get; set; } = "#7C3AED";
        public string BackgroundImagePath    { get; set; } = "";
        public bool   IsTurboEnabled         { get; set; } = false;
        
        // Legacy support
        public string SessionToken       { get; set; } = "";
        public string SessionUuid        { get; set; } = "";
        public string LastServerIp       { get; set; } = "nebula.net";
        public int    CurrentProfileIdx  { get; set; } = 0;
    }

    public class MinecraftProfile
    {
        public string Id             { get; set; } = Guid.NewGuid().ToString("N");
        public string Name           { get; set; } = "Nueva Instancia";
        public string Icon           { get; set; } = "🚀";
        public string Version        { get; set; } = "1.20.1";
        public string LastVersion    { get; set; } = "";
        public string LoaderType     { get; set; } = "vanilla"; // vanilla, forge, fabric, neoforge
        public string LoaderVersion  { get; set; } = "";
        public string JavaPath       { get; set; } = "";
        public int    RamGB          { get; set; } = 4;
        public string CreatedAt      { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }
}
