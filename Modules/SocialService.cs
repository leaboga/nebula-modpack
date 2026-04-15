using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KrakenLauncher.Modules
{
    public class ServerInfo
    {
        public string Motd { get; set; } = "Cerrado";
        public int OnlinePlayers { get; set; }
        public int MaxPlayers { get; set; }
        public string Version { get; set; } = "N/A";
        public bool IsOnline { get; set; } = false;
        public long Ping { get; set; } = 0;
        public double HostCpu { get; set; }
        public double HostRam { get; set; }
        public List<string> Players { get; set; } = new();
    }

    public class SocialFeedItem
    {
        public string type   { get; set; } = "";
        public string player { get; set; } = "";
        public string text   { get; set; } = "";
        public string date   { get; set; } = "";
        public string head   { get; set; } = "";
    }

    public class SocialService
    {
        private readonly HttpClient _http;

        public SocialService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _http.DefaultRequestHeaders.Add("User-Agent", "KrakenLauncher/5.0");
        }

        /// <summary>Pings the Minecraft server and fetches the social feed JSON, with full error handling.</summary>
        public async Task<ServerInfo> GetServerStatus(string address, int port = 25565)
        {
            var info = new ServerInfo();

            // ── MC Protocol ping ─────────────────────────────────────────
            try
            {
                using var cts    = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var client = new TcpClient();

                var connectTask = client.ConnectAsync(address, port);
                if (await Task.WhenAny(connectTask, Task.Delay(2000, cts.Token)) == connectTask && client.Connected)
                {
                    using var stream        = client.GetStream();
                    stream.ReadTimeout  = 2000;
                    stream.WriteTimeout = 2000;

                    await WritePacket(stream, CreateHandshake(address, port));
                    await WritePacket(stream, new byte[] { 0x00 });

                    // Read packet length + packet id
                    ReadVarInt(stream);
                    ReadVarInt(stream);

                    int jsonLen = ReadVarInt(stream);
                    if (jsonLen > 0 && jsonLen < 100_000)
                    {
                        byte[] buffer = new byte[jsonLen];
                        int read = 0;
                        using var readCts = new CancellationTokenSource(2000);
                        while (read < jsonLen)
                        {
                            int r = await stream.ReadAsync(buffer, read, jsonLen - read, readCts.Token);
                            if (r == 0) break;
                            read += r;
                        }

                        if (read == jsonLen)
                        {
                            var data = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(buffer));
                            if (data != null)
                            {
                                info.IsOnline      = true;
                                info.OnlinePlayers = (int)(data.players?.online   ?? 0);
                                info.MaxPlayers    = (int)(data.players?.max      ?? 0);
                                info.Version       = (string)(data.version?.name  ?? "N/A");

                                if (data.players?.sample != null)
                                {
                                    foreach (var p in data.players.sample)
                                    {
                                        string name = (string)(p.name ?? "");
                                        if (!string.IsNullOrEmpty(name)) info.Players.Add(name);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { /* timeout — server offline */ }
            catch { /* socket error — server offline */ }

            // ── Social feed JSON (GitHub) ─────────────────────────────────
            try
            {
                using var http   = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                string    url    = $"https://raw.githubusercontent.com/leaboga/nebula-modpack/main/social_feed.json?t={DateTime.Now.Ticks}";
                string    json   = await http.GetStringAsync(url);
                var       extra  = JsonConvert.DeserializeObject<dynamic>(json);

                if (extra != null)
                {
                    info.HostCpu = (double)(extra.server_cpu  ?? 0.0);
                    info.HostRam = (double)(extra.server_ram  ?? 0.0);

                    // Heartbeat: consider online if last HB was within 5 min
                    long lastHb = (long)(extra.last_heartbeat ?? 0L);
                    if (lastHb > 0 && Math.Abs(DateTime.Now.Ticks - lastHb) < TimeSpan.FromMinutes(5).Ticks)
                        info.IsOnline = true;
                }
            }
            catch { /* network not available or file missing */ }

            return info;
        }

        public async Task<List<SocialFeedItem>> GetRecentFeedFromWeb()
        {
            try
            {
                string url  = $"https://raw.githubusercontent.com/leaboga/nebula-modpack/main/social_feed.json?t={DateTime.Now.Ticks}";
                string json = await _http.GetStringAsync(url);
                var    data = JsonConvert.DeserializeObject<dynamic>(json);

                if (data?.feed != null)
                    return JsonConvert.DeserializeObject<List<SocialFeedItem>>(data.feed.ToString())
                           ?? new List<SocialFeedItem>();
            }
            catch { }
            return new List<SocialFeedItem>();
        }

        // ── Minecraft protocol helpers ────────────────────────────────────
        private byte[] CreateHandshake(string host, int port)
        {
            using var ms   = new MemoryStream();
            ms.WriteByte(0x00);
            WriteVarInt(ms, -1);                                    // protocol version (-1 = status)
            byte[] addr = Encoding.UTF8.GetBytes(host);
            WriteVarInt(ms, addr.Length);
            ms.Write(addr, 0, addr.Length);
            ms.WriteByte((byte)(port >> 8));
            ms.WriteByte((byte)port);
            WriteVarInt(ms, 1);                                     // next state = status
            return ms.ToArray();
        }

        private async Task WritePacket(Stream s, byte[] data)
        {
            WriteVarInt(s, data.Length);
            await s.WriteAsync(data, 0, data.Length);
        }

        private void WriteVarInt(Stream s, int value)
        {
            uint v = (uint)value;
            while (v >= 128) { s.WriteByte((byte)(v | 128)); v >>= 7; }
            s.WriteByte((byte)v);
        }

        private int ReadVarInt(Stream s)
        {
            int result = 0, shift = 0;
            byte b;
            do
            {
                int raw = s.ReadByte();
                if (raw == -1) throw new EndOfStreamException("Unexpected end of stream reading VarInt");
                b = (byte)raw;
                result |= (b & 0x7F) << shift;
                shift  += 7;
                if (shift > 35) throw new InvalidDataException("VarInt too long");
            }
            while ((b & 0x80) != 0);
            return result;
        }
    }
}
