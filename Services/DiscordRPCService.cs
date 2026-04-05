using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace NebulaLauncher.Services
{
    public class DiscordRPCService : IDisposable
    {
        private const string ClientId = "1234567890123456"; // Replace with real Discord App Client ID
        private DiscordRpcClient? _client;
        private bool _initialized = false;

        public void Initialize()
        {
            try
            {
                _client = new DiscordRpcClient(ClientId)
                {
                    Logger = new NullLogger()
                };
                _client.OnReady    += (s, e) => _initialized = true;
                _client.OnError    += (s, e) => { };
                _client.Initialize();
            }
            catch { /* Discord not installed or not running */ }
        }

        public void SetPresence(string details, string state, int onlinePlayers = 0, int maxPlayers = 0)
        {
            if (_client == null || !_initialized) return;
            try
            {
                _client.SetPresence(new RichPresence
                {
                    Details = details,
                    State   = state,
                    Assets  = new Assets
                    {
                        LargeImageKey  = "nebula_logo",
                        LargeImageText = "Nebula Launcher",
                        SmallImageKey  = "minecraft_icon",
                        SmallImageText = "Minecraft"
                    },
                    Party = onlinePlayers > 0 ? new Party
                    {
                        ID      = "nebula_server",
                        Size    = onlinePlayers,
                        Max     = maxPlayers > 0 ? maxPlayers : 20
                    } : null,
                    Timestamps = Timestamps.Now
                });
            }
            catch { }
        }

        public void SetInGame(string username, int onlinePlayers, int maxPlayers)
            => SetPresence($"Jugando como {username}", $"Servidor Nebula", onlinePlayers, maxPlayers);

        public void SetActivity(string task)
            => SetPresence(task, "Nebula Launcher");

        public void SetIdle()
            => SetActivity("En el menú principal");

        public void Dispose()
        {
            try { _client?.Dispose(); }
            catch { }
        }
    }
}
