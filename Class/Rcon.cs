using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace hlsmx
{
    static class Rcon
    {
        public static string password_from_params(string srv_params)
        {
            Match m = Regex.Match(srv_params ?? "", "\\+rcon_password\\s+(?:\"([^\"]*)\"|(\\S+))");
            if (!m.Success) { return ""; }
            return m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        }

        private static readonly HashSet<string> goldsrc_servers = new HashSet<string>();

        public static bool is_goldsrc(string ip, int port)
        {
            try { return is_goldsrc(new IPEndPoint(IPAddress.Parse(ip), port)); }
            catch { return false; }
        }

        private static bool is_goldsrc(IPEndPoint ep)
        {
            string key = ep.ToString();
            lock (goldsrc_servers) { if (goldsrc_servers.Contains(key)) { return true; } }
            try
            {
                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    s.ReceiveTimeout = 1000;
                    s.SendTo(oob("challenge rcon\n"), ep);
                    byte[] buffer = new byte[1400];
                    int len = s.Receive(buffer);
                    if (!Encoding.ASCII.GetString(buffer, 4, Math.Max(0, len - 4)).Contains("challenge rcon")) { return false; }
                }
            }
            catch { return false; }
            lock (goldsrc_servers) { goldsrc_servers.Add(key); }
            return true;
        }

        public static void forget(string ip, int port)
        {
            IPAddress address;
            if (!IPAddress.TryParse(ip ?? "", out address)) { return; }
            lock (goldsrc_servers) { goldsrc_servers.Remove(new IPEndPoint(address, port).ToString()); }
        }

        public static bool send(string ip, int port, string password, string command)
        {
            if (string.IsNullOrEmpty(password) || port < 1) { return false; }
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                return is_goldsrc(ep) ? send_goldsrc(ep, password, command) : send_source(tcp_endpoint(ep), password, command);
            }
            catch { return false; }
        }

        public static bool say(string ip, int port, string password, string message)
        {
            string text = Regex.Replace(message ?? "", "[\"';\r\n]", " ").Trim();
            return text.Length > 0 && send(ip, port, password, "say " + text);
        }

        private static bool send_goldsrc(IPEndPoint ep, string password, string command)
        {
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                return send_goldsrc(s, ep, password, command);
            }
        }

        private static bool send_goldsrc(Socket s, IPEndPoint ep, string password, string command)
        {
            s.ReceiveTimeout = 2000;
            s.SendTo(oob("challenge rcon\n"), ep);
            byte[] buffer = new byte[1400];
            int len = s.Receive(buffer);
            string reply = Encoding.ASCII.GetString(buffer, 4, Math.Max(0, len - 4));
            Match m = Regex.Match(reply, "challenge rcon (\\d+)");
            if (!m.Success) { return false; }
            s.SendTo(oob(string.Format("rcon {0} \"{1}\" {2}\n", m.Groups[1].Value, password, command)), ep);
            return true;
        }

        public static string query_goldsrc(string ip, int port, string password, string command)
        {
            if (string.IsNullOrEmpty(password) || port < 1) { return null; }
            try
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    if (!send_goldsrc(s, ep, password, command)) { return null; }
                    StringBuilder text = new StringBuilder();
                    byte[] buffer = new byte[4096];
                    try
                    {
                        while (true)
                        {
                            int len = s.Receive(buffer);
                            if (len > 5 && buffer[4] == 0x6C) { text.Append(Encoding.UTF8.GetString(buffer, 5, len - 5).TrimEnd('\0')); }
                            s.ReceiveTimeout = 300;
                        }
                    }
                    catch (SocketException) { }
                    return text.Length > 0 ? text.ToString() : null;
                }
            }
            catch { return null; }
        }

        public static int count_hltv(string status)
        {
            return Regex.Matches(status ?? "", "^#\\s*\\d+\\s+\".*\"\\s+\\d+\\s+HLTV\\s", RegexOptions.Multiline).Count;
        }

        private static byte[] oob(string text)
        {
            List<byte> bytes = new List<byte> { 0xff, 0xff, 0xff, 0xff };
            bytes.AddRange(Encoding.UTF8.GetBytes(text));
            return bytes.ToArray();
        }

        private static IPEndPoint tcp_endpoint(IPEndPoint ep)
        {
            if (!IPAddress.IsLoopback(ep.Address) && !ep.Address.Equals(IPAddress.Any)) { return ep; }
            IPAddress bound = NetQuery.Instance.tcp_listener(ep.Port);
            return bound == null || bound.Equals(IPAddress.Any) ? ep : new IPEndPoint(bound, ep.Port);
        }

        private static bool send_source(IPEndPoint ep, string password, string command)
        {
            using (TcpClient client = new TcpClient())
            {
                NetworkStream stream = auth_source(client, ep, password);
                if (stream == null) { return false; }
                write_packet(stream, 2, 2, command);
                return true;
            }
        }

        private static NetworkStream auth_source(TcpClient client, IPEndPoint ep, string password)
        {
            IAsyncResult connect = client.BeginConnect(ep.Address, ep.Port, null, null);
            if (!connect.AsyncWaitHandle.WaitOne(2000)) { return null; }
            client.EndConnect(connect);
            client.ReceiveTimeout = 2000;
            client.SendTimeout = 2000;
            NetworkStream stream = client.GetStream();

            write_packet(stream, 1, 3, password);
            for (int i = 0; i < 8; i++)
            {
                int id, type;
                read_packet(stream, out id, out type);
                if (type == 2) { return id == -1 ? null : stream; }
            }
            return null;
        }

        private static void write_packet(NetworkStream stream, int id, int type, string body)
        {
            byte[] text = Encoding.UTF8.GetBytes(body);
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                w.Write(text.Length + 10);
                w.Write(id);
                w.Write(type);
                w.Write(text);
                w.Write((short)0);
                byte[] packet = ms.ToArray();
                stream.Write(packet, 0, packet.Length);
            }
        }

        private static void read_packet(NetworkStream stream, out int id, out int type)
        {
            BinaryReader r = new BinaryReader(stream);
            int size = r.ReadInt32();
            id = r.ReadInt32();
            type = r.ReadInt32();
            r.ReadBytes(size - 8);
        }
    }
}
