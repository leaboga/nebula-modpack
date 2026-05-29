using System;
using System.Threading.Tasks;
using KrakenLauncher.Modules;

namespace KrakenLauncher.Services
{
    public class GameLaunchManager
    {
        private readonly string _gameFolder;
        private readonly UserSession _session;

        public event Action<string>? OnLog;
        public event Action<double>? OnProgress;

        public GameLaunchManager(string gameFolder, UserSession session)
        {
            _gameFolder = gameFolder;
            _session = session;
        }

        public async Task<int> LaunchMinecraftAsync(MinecraftProfile profile)
        {
            try
            {
                var mcLauncher = new McGameLauncher(
                    _gameFolder, 
                    profile.RamGB, 
                    _session.Username,
                    _session.AuthMode == "premium", 
                    profile.Version, 
                    profile.LoaderType,
                    profile.LoaderVersion, 
                    manualJavaPath: profile.JavaPath,
                    customSplash: _session.CustomSplashText,
                    isOverlay: _session.IsOverlayEnabled,
                    isTurboEnabled: _session.IsTurboEnabled
                );

                mcLauncher.OnLog += msg => OnLog?.Invoke(msg);
                mcLauncher.OnProgress += pct => OnProgress?.Invoke(pct);
                
                try { System.Media.SystemSounds.Exclamation.Play(); } catch (Exception ex) { Logger.LogError("Error playing exclamation sound", ex); }

                return await mcLauncher.LaunchAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error en GameLaunchManager", ex);
                return -1;
            }
        }
    }
}
