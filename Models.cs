using System;
using System.Collections.Generic;

namespace KrakenLauncher
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
        public string AccentColor            { get; set; } = "#00F2FF";
        public string BackgroundImagePath    { get; set; } = "";
        public bool   IsTurboEnabled         { get; set; } = false;
        public bool   SkipConfigSync         { get; set; } = false;
        public bool   HasFinishedDiscovery   { get; set; } = false; // Tutorial state
        public int    DiscoveryStep          { get; set; } = 0;

        // Customization & Features
        public string CloudPath              { get; set; } = "";
        public string CustomSplashText       { get; set; } = "";
        public bool   IsOverlayEnabled       { get; set; } = false;
        
        // Config sync tracking
        public string LastAppliedConfigHash { get; set; } = "";    // Hash de las ultimas configs aplicadas
        public Dictionary<string, string> AppliedConfigVersions { get; set; } = new(); // PerfilId -> Version de Config
        public Dictionary<string, string> RejectedConfigVersions { get; set; } = new(); // PerfilId -> Version de Config rechazada

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
        public string Icon           { get; set; } = "K";
        public string Version        { get; set; } = "1.20.1";
        public string LastVersion    { get; set; } = "";
        public string LoaderType     { get; set; } = "vanilla"; // vanilla, forge, fabric, neoforge
        public string LoaderVersion  { get; set; } = "";
        public string JavaPath       { get; set; } = "";
        public string JvmArgs        { get; set; } = McGameLauncher.DefaultJvmArgs;
        public int    RamGB          { get; set; } = 4;
        public string CreatedAt      { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string LastPlayedAt   { get; set; } = "Nunca";
        public long   TotalPlayTimeMinutes { get; set; } = 0;
        public string LastSyncDate   { get; set; } = "Nunca";
        public string LastSyncHash   { get; set; } = "";
        public string ModpackId      { get; set; } = "";
        public bool   SyncWithServer { get; set; } = false;
    }
}
