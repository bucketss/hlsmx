using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace hlsmx
{
    class NetQuery
    {
        private static readonly Lazy<NetQuery> shared = new Lazy<NetQuery>(() => new NetQuery());
        public static NetQuery Instance { get { return shared.Value; } }
        private NetQuery() { }

        public class A2sInfo
        {
            public bool Ok;
            public string Error = "";
            public bool Legacy;
            public int Protocol;
            public string Address = "";
            public string Name = "";
            public string Map = "";
            public string Folder = "";
            public string Game = "";
            public int AppId;
            public int Players;
            public int MaxPlayers;
            public int Bots;
            public char ServerType;
            public char Environment;
            public bool Password;
            public bool Vac;
            public string Version = "";
            public int ShipMode = -1;
            public int ShipWitnesses;
            public int ShipDuration;
            public int GamePort;
            public ulong SteamId;
            public bool SourceTv;
            public int TvPort;
            public string TvName = "";
            public string Keywords = "";
            public ulong GameId;
            public bool Mod;
            public string ModLink = "";
            public string ModDownload = "";
            public int ModVersion;
            public int ModSize;
            public bool ModMultiplayerOnly;
            public bool ModCustomDll;
        }

        public class UdpOwner
        {
            public uint Address;
            public int Port;
            public int Pid;
        }

        private string lan_address;
        public string localhost
        {
            get
            {
                if (lan_address == null) { lan_address = first_lan_ipv4(); }
                return lan_address;
            }
        }
        private HashSet<string> local_addresses;
        public bool is_local(string ip)
        {
            IPAddress address;
            if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out address)) { return false; }
            if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any)) { return true; }
            if (local_addresses == null)
            {
                HashSet<string> set = new HashSet<string>();
                try { foreach (IPAddress a in Dns.GetHostAddresses(Dns.GetHostName())) { set.Add(a.ToString()); } }
                catch { }
                local_addresses = set;
            }
            return local_addresses.Contains(address.ToString());
        }

        [DllImport("iphlpapi.dll")]
        static extern uint GetExtendedUdpTable(IntPtr table, ref int size, bool sort, int family, int table_class, uint reserved);

        public List<UdpOwner> udp_table()
        {
            List<UdpOwner> rows = new List<UdpOwner>();
            int size = 0;
            GetExtendedUdpTable(IntPtr.Zero, ref size, false, 2, 1, 0);
            for (int attempt = 0; attempt < 3 && size > 0; attempt++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    uint result = GetExtendedUdpTable(buffer, ref size, false, 2, 1, 0);
                    if (result == 122) { continue; }
                    if (result != 0) { return rows; }
                    int count = Marshal.ReadInt32(buffer);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr row = new IntPtr(buffer.ToInt64() + 4 + i * 12);
                        UdpOwner owner = new UdpOwner();
                        owner.Address = (uint)Marshal.ReadInt32(row);
                        uint raw_port = (uint)Marshal.ReadInt32(row, 4);
                        owner.Port = (int)(((raw_port & 0xFF) << 8) | ((raw_port >> 8) & 0xFF));
                        owner.Pid = Marshal.ReadInt32(row, 8);
                        rows.Add(owner);
                    }
                    return rows;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            return rows;
        }

        [DllImport("iphlpapi.dll")]
        static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool sort, int family, int table_class, uint reserved);

        public IPAddress tcp_listener(int port)
        {
            int size = 0;
            GetExtendedTcpTable(IntPtr.Zero, ref size, false, 2, 3, 0);
            for (int attempt = 0; attempt < 3 && size > 0; attempt++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    uint result = GetExtendedTcpTable(buffer, ref size, false, 2, 3, 0);
                    if (result == 122) { continue; }
                    if (result != 0) { return null; }
                    int count = Marshal.ReadInt32(buffer);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr row = new IntPtr(buffer.ToInt64() + 4 + i * 24);
                        uint raw_port = (uint)Marshal.ReadInt32(row, 8);
                        if ((int)(((raw_port & 0xFF) << 8) | ((raw_port >> 8) & 0xFF)) != port) { continue; }
                        return new IPAddress((long)(uint)Marshal.ReadInt32(row, 4));
                    }
                    return null;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            return null;
        }

        public static int port_owner(List<UdpOwner> table, string ip, int port)
        {
            IPAddress wanted;
            bool any = !IPAddress.TryParse(ip ?? "", out wanted) || IPAddress.IsLoopback(wanted) || wanted.Equals(IPAddress.Any);
            foreach (UdpOwner row in table)
            {
                if (row.Port != port) { continue; }
                if (row.Address == 0 || any || new IPAddress(row.Address).Equals(wanted)) { return row.Pid; }
            }
            return 0;
        }

        public int udp_port_owner(string ip, int port)
        {
            return port_owner(udp_table(), ip, port);
        }

        private static string first_lan_ipv4()
        {
            try
            {
                IPAddress found = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Select(u => u.Address)
                    .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
                if (found != null) { return found.ToString(); }
            }
            catch { }
            return IPAddress.Loopback.ToString();
        }

        private static readonly byte[] info_request = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }.Concat(Encoding.ASCII.GetBytes("TSource Engine Query\0")).ToArray();
        private static readonly byte[] player_request = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };

        public A2sInfo a2sinfo(string ip, int port)
        {
            A2sInfo result = a2sinfo_once(ip, port);
            IPAddress address;
            if (!result.Ok && IPAddress.TryParse(ip, out address) && IPAddress.IsLoopback(address) && localhost != "127.0.0.1")
            {
                A2sInfo retry = a2sinfo_once(localhost, port);
                if (retry.Ok) { return retry; }
            }
            return result;
        }

        private static readonly byte[] details_request = oob("details\0");
        private static readonly byte[] legacy_info_request = oob("info\0");
        private static readonly byte[] legacy_player_request = oob("players\0");
        private readonly Dictionary<string, bool> query_modes = new Dictionary<string, bool>();

        private static byte[] oob(string text)
        {
            return new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }.Concat(Encoding.ASCII.GetBytes(text)).ToArray();
        }

        private bool? query_mode(string ip, int port)
        {
            bool legacy;
            lock (query_modes) { return query_modes.TryGetValue(ip + ":" + port, out legacy) ? legacy : (bool?)null; }
        }

        private void set_query_mode(string ip, int port, bool legacy)
        {
            lock (query_modes) { query_modes[ip + ":" + port] = legacy; }
        }

        public void forget(string ip, int port)
        {
            lock (query_modes)
            {
                query_modes.Remove(ip + ":" + port);
                query_modes.Remove(localhost + ":" + port);
            }
        }

        public List<string> a2splayers(string ip, int port)
        {
            try
            {
                byte[] reply = query_mode(ip, port) == true ? exchange(ip, port, legacy_player_request, false) : exchange(ip, port, player_request, false);
                if (reply == null || reply[4] != 0x44) { return null; }
                Reader r = new Reader(reply, 5);
                int count = r.u8();
                List<string> names = new List<string>();
                for (int i = 0; i < count && r.more; i++)
                {
                    r.u8();
                    names.Add(r.cstr());
                    r.skip(8);
                }
                return names;
            }
            catch { return null; }
        }

        private A2sInfo a2sinfo_once(string ip, int port)
        {
            bool? mode = query_mode(ip, port);
            if (mode.HasValue) { return mode.Value ? query_legacy(ip, port) : query_modern(ip, port); }
            A2sInfo info = query_modern(ip, port);
            if (info.Ok) { set_query_mode(ip, port, false); return info; }
            A2sInfo legacy = query_legacy(ip, port);
            if (!legacy.Ok) { return info; }
            set_query_mode(ip, port, true);
            return legacy;
        }

        private A2sInfo query_modern(string ip, int port)
        {
            A2sInfo info = new A2sInfo();
            try { parse_info(exchange(ip, port, info_request, true), info); }
            catch (Exception e) { info.Ok = false; info.Error = e.Message; }
            return info;
        }

        private A2sInfo query_legacy(string ip, int port)
        {
            A2sInfo info = new A2sInfo();
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                using (Socket socks = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socks.ReceiveTimeout = 1000;
                    socks.SendTo(details_request, ep);
                    socks.SendTo(legacy_info_request, ep);
                    for (int i = 0; i < 2 && !info.Ok; i++)
                    {
                        byte[] reply = receive(socks);
                        if (reply != null && reply.Length >= 6 && (reply[4] == 0x6D || reply[4] == 0x43)) { parse_info(reply, info); }
                    }
                }
            }
            catch (Exception e) { if (!info.Ok) { info.Error = e.Message; } }
            return info;
        }

        private static void parse_info(byte[] reply, A2sInfo info)
        {
            if (reply == null) { info.Error = "bad_reply"; return; }
            if (reply[4] == 0x49) { parse_source(reply, info); }
            else if (reply[4] == 0x6D || reply[4] == 0x43) { parse_goldsrc(reply, info); }
            else { info.Error = "bad_reply"; }
        }

        private static byte[] exchange(string ip, int port, byte[] request, bool append_challenge)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
            using (Socket socks = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socks.ReceiveTimeout = 1000;
                socks.SendTo(request, ep);
                byte[] reply = receive(socks);
                if (reply != null && reply.Length >= 9 && reply[4] == 0x41)
                {
                    byte[] retry = append_challenge ? new byte[request.Length + 4] : (byte[])request.Clone();
                    Array.Copy(request, retry, request.Length);
                    Array.Copy(reply, 5, retry, retry.Length - 4, 4);
                    socks.SendTo(retry, ep);
                    reply = receive(socks);
                }
                if (reply == null || reply.Length < 6) { return null; }
                return reply;
            }
        }

        private static byte[] receive(Socket socks)
        {
            byte[] buffer = new byte[4096];
            int length = socks.Receive(buffer);
            if (length >= 4 && is_oob(buffer)) { return slice(buffer, 0, length); }
            if (length < 10 || !is_split(buffer)) { return null; }
            int id = BitConverter.ToInt32(buffer, 4);
            bool goldsrc = (buffer[8] >> 4) > 0 || buffer[9] >= buffer[8];
            int header = goldsrc ? 9 : 12;
            int total = goldsrc ? buffer[8] & 0x0F : buffer[8];
            if (id < 0 || total < 1 || length < header) { return null; }
            byte[][] parts = new byte[total][];
            int received = 0;
            while (true)
            {
                if (length >= header && is_split(buffer) && BitConverter.ToInt32(buffer, 4) == id && (goldsrc ? buffer[8] & 0x0F : buffer[8]) == total)
                {
                    int number = goldsrc ? buffer[8] >> 4 : buffer[9];
                    if (number < total && parts[number] == null)
                    {
                        parts[number] = slice(buffer, header, length - header);
                        received++;
                    }
                }
                if (received == total) { break; }
                length = socks.Receive(buffer);
            }
            byte[] reply = parts.SelectMany(p => p).ToArray();
            return reply.Length >= 4 && is_oob(reply) ? reply : null;
        }

        private static byte[] slice(byte[] data, int offset, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, offset, result, 0, length);
            return result;
        }

        private static bool is_oob(byte[] data)
        {
            return data[0] == 0xFF && data[1] == 0xFF && data[2] == 0xFF && data[3] == 0xFF;
        }

        private static bool is_split(byte[] data)
        {
            return data[0] == 0xFE && data[1] == 0xFF && data[2] == 0xFF && data[3] == 0xFF;
        }

        private static void parse_source(byte[] data, A2sInfo info)
        {
            Reader r = new Reader(data, 5);
            info.Protocol = r.u8();
            info.Name = r.cstr();
            info.Map = r.cstr();
            info.Folder = r.cstr();
            info.Game = r.cstr();
            info.AppId = r.u16();
            info.Players = r.u8();
            info.MaxPlayers = r.u8();
            info.Ok = true;
            try
            {
                info.Bots = r.u8();
                info.ServerType = (char)r.u8();
                info.Environment = (char)r.u8();
                info.Password = r.u8() != 0;
                info.Vac = r.u8() != 0;
                if (info.AppId == 2400)
                {
                    info.ShipMode = r.u8();
                    info.ShipWitnesses = r.u8();
                    info.ShipDuration = r.u8();
                }
                info.Version = r.cstr();
                if (!r.more) { return; }
                int edf = r.u8();
                if ((edf & 0x80) != 0) { info.GamePort = r.u16(); }
                if ((edf & 0x10) != 0) { info.SteamId = r.u64(); }
                if ((edf & 0x40) != 0)
                {
                    info.SourceTv = true;
                    info.TvPort = r.u16();
                    info.TvName = r.cstr();
                }
                if ((edf & 0x20) != 0) { info.Keywords = r.cstr(); }
                if ((edf & 0x01) != 0) { info.GameId = r.u64(); }
            }
            catch { }
        }

        private static void parse_goldsrc(byte[] data, A2sInfo info)
        {
            Reader r = new Reader(data, 5);
            info.Legacy = true;
            info.Address = r.cstr();
            info.Name = r.cstr();
            info.Map = r.cstr();
            info.Folder = r.cstr();
            info.Game = r.cstr();
            info.Players = r.u8();
            info.MaxPlayers = r.u8();
            info.Ok = true;
            try
            {
                info.Protocol = r.u8();
                info.ServerType = char.ToLowerInvariant((char)r.u8());
                info.Environment = char.ToLowerInvariant((char)r.u8());
                info.Password = r.u8() != 0;
                info.Mod = r.u8() == 1;
                if (info.Mod)
                {
                    info.ModLink = r.cstr();
                    info.ModDownload = r.cstr();
                    r.u8();
                    info.ModVersion = r.i32();
                    info.ModSize = r.i32();
                    info.ModMultiplayerOnly = r.u8() == 1;
                    info.ModCustomDll = r.u8() == 1;
                }
                info.Vac = r.u8() != 0;
                info.Bots = r.u8();
            }
            catch { }
        }

        class Reader
        {
            private readonly byte[] data;
            private int pos;
            public Reader(byte[] data, int pos) { this.data = data; this.pos = pos; }
            public bool more { get { return pos < data.Length; } }
            public int u8()
            {
                if (pos >= data.Length) { throw new IndexOutOfRangeException(); }
                return data[pos++];
            }
            public int u16() { int lo = u8(); return lo | (u8() << 8); }
            public int i32()
            {
                skip(4);
                return BitConverter.ToInt32(data, pos - 4);
            }
            public ulong u64()
            {
                skip(8);
                return BitConverter.ToUInt64(data, pos - 8);
            }
            public void skip(int count)
            {
                if (pos + count > data.Length) { throw new IndexOutOfRangeException(); }
                pos += count;
            }
            public string cstr()
            {
                int end = Array.IndexOf(data, (byte)0, pos);
                if (end < 0) { throw new IndexOutOfRangeException(); }
                string text = Encoding.UTF8.GetString(data, pos, end - pos);
                pos = end + 1;
                return text;
            }
        }
    }
}
