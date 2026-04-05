using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using NebulaLauncher.Services;

namespace NebulaLauncher.Modules
{
    public partial class SocialView : UserControl
    {
        private readonly SocialService      _socialService;
        private readonly ServerStatusCache  _cache;
        private readonly string             _serverIp;
        private readonly string             _username;
        private readonly DispatcherTimer    _timer;
        private bool _isRefreshing = false;

        // Theme brushes
        private static readonly SolidColorBrush BrushOnline  = new(Color.FromRgb(0x10, 0xB9, 0x81));
        private static readonly SolidColorBrush BrushOffline = new(Color.FromRgb(0xEF, 0x44, 0x44));
        private static readonly SolidColorBrush BrushFg      = new(Color.FromRgb(0xF0, 0xEA, 0xFF));
        private static readonly SolidColorBrush BrushMuted   = new(Color.FromRgb(0x4A, 0x42, 0x66));
        private static readonly SolidColorBrush BrushAccent  = new(Color.FromRgb(0xA7, 0x8B, 0xFA));

        public SocialView(string serverIp, string username)
        {
            InitializeComponent();
            _serverIp      = serverIp;
            _username      = username;
            _socialService = new SocialService();
            _cache         = new ServerStatusCache();

            // Show cached data immediately while loading
            ApplyCachedStatus();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _timer.Tick += async (s, e) => await RefreshAll();
            _timer.Start();

            _ = RefreshAll();
        }

        // ── Apply cached data immediately (offline-first) ─────────────────
        private void ApplyCachedStatus()
        {
            var cached = _cache.Load();
            if (!cached.HasData) return;
            string lastSeen = _cache.GetLastSeenLabel(cached.LastSeen);
            ApplyStatusToUI(cached.Status, isCache: true, cacheLabel: lastSeen);
        }

        private async Task RefreshAll()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;
            try
            {
                var status = await _socialService.GetServerStatus(_serverIp);
                var feed   = await _socialService.GetRecentFeedFromWeb();

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        bool online = status?.IsOnline == true;

                        if (online)
                        {
                            // Save to cache only when actually online
                            _cache.Save(status!);
                            ApplyStatusToUI(status!, isCache: false, cacheLabel: "");
                        }
                        else
                        {
                            // Show cached data with stale label
                            var cached = _cache.Load();
                            if (cached.HasData)
                            {
                                string lastSeen = _cache.GetLastSeenLabel(cached.LastSeen);
                                ApplyStatusToUI(cached.Status, isCache: true, cacheLabel: lastSeen);

                                // But force the dot/status offline
                                if (StatusDot  != null) StatusDot.Fill = BrushOffline;
                                if (StatusText != null) { StatusText.Text = "OFFLINE"; StatusText.Foreground = BrushOffline; }
                            }
                            else
                            {
                                ApplyStatusToUI(new ServerInfo(), isCache: false, cacheLabel: "");
                            }
                        }

                        // Merge global feed with local session history
                        var combinedFeed = new List<SocialFeedItem>();
                        
                        // Add local recent session if exists
                        var history    = new SessionHistoryService().Load();
                        if (history.Sessions.Count > 0)
                        {
                            var last = history.Sessions[history.Sessions.Count - 1];
                            combinedFeed.Add(new SocialFeedItem {
                                player = "Tú (Local)",
                                text   = $"jugaste durante {last.Duration} minutos.",
                                date   = last.Date.ToString("HH:mm"),
                                type   = "local",
                                head   = $"https://mc-heads.net/avatar/{_username}/32"
                            });
                        }

                        if (feed != null) combinedFeed.AddRange(feed);

                        if (AdvancementsList != null && combinedFeed.Count > 0)
                            AdvancementsList.ItemsSource = combinedFeed;
                    }
                    catch { }
                });
            }
            catch { }
            finally { _isRefreshing = false; }
        }

        private void ApplyStatusToUI(ServerInfo status, bool isCache, string cacheLabel)
        {
            bool online = status.IsOnline;

            // Dot glow
            if (StatusDot != null)
            {
                StatusDot.Fill = online ? BrushOnline : BrushOffline;
                StatusDot.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color       = online ? Color.FromRgb(0x10, 0xB9, 0x81) : Color.FromRgb(0xEF, 0x44, 0x44),
                    Opacity     = 0.9, BlurRadius = 10, ShadowDepth = 0
                };
            }

            if (StatusText != null)
            {
                StatusText.Text       = online ? "ONLINE" : "OFFLINE";
                StatusText.Foreground = online ? BrushFg : BrushOffline;
            }

            if (PlayerCountText != null)
                PlayerCountText.Text = online
                    ? $"{status.OnlinePlayers} / {status.MaxPlayers} Jugadores"
                    : isCache && !string.IsNullOrEmpty(cacheLabel) ? $"Último dato: {cacheLabel}" : "Servidor cerrado";

            if (VersionText != null)
                VersionText.Text = !string.IsNullOrEmpty(status.Version) && status.Version != "N/A"
                    ? $"v{status.Version}"
                    : "NeoForge 1.21.1";

            // Resources — show cached values grayed out if stale
            double cpu = Math.Clamp(status.HostCpu, 0, 100);
            double ram = Math.Clamp(status.HostRam, 0, 64);

            var resourceColor = isCache ? BrushMuted : BrushAccent;

            if (CpuText != null) { CpuText.Text = $"{(int)cpu}%"; CpuText.Foreground = resourceColor; }
            if (CpuBar  != null) CpuBar.Value   = cpu;

            if (RamText != null) { RamText.Text = $"{ram:F1} GB"; RamText.Foreground = resourceColor; }
            if (RamBar  != null) RamBar.Value   = (ram / 16.0) * 100;

            // Cache badge
            if (CacheBadge != null)
                CacheBadge.Visibility = isCache && cached_HasData()
                    ? Visibility.Visible : Visibility.Collapsed;
            if (CacheLabel != null && isCache)
                CacheLabel.Text = $"📦 Datos en caché · {cacheLabel}";
        }

        // Small helper to avoid loading cache twice
        private bool cached_HasData() => _cache.Load().HasData;

        public void Stop() => _timer.Stop();
    }
}
