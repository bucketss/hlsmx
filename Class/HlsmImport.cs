using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hlsmx
{
    static class HlsmImport
    {
        private const int record_size = 1770;

        public static List<ServerEntry> read_servers(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length == 0 || data.Length % record_size != 0) { throw new InvalidDataException("Not an HLSM server list."); }
            string cores = string.Concat(Enumerable.Range(0, Environment.ProcessorCount).Select(i => i + "|"));
            List<ServerEntry> result = new List<ServerEntry>();
            for (int offset = 0; offset < data.Length; offset += record_size)
            {
                ServerEntry s = new ServerEntry();
                s.Name = wide(data, offset, 0x40);
                s.IP = Encoding.ASCII.GetString(data, offset + 0x40, 0x10).Split('\0')[0];
                s.Port = BitConverter.ToUInt16(data, offset + 0x50).ToString();
                s.Executable = wide(data, offset + 0x54, 0x25E - 0x54);
                s.Params = wide(data, offset + 0x25E, 0x400);
                s.Cores = cores;
                s.Paused = true;
                if (s.IP.Length == 0) { s.IP = "127.0.0.1"; }
                result.Add(s);
            }
            return result;
        }

        public static Dictionary<string, int> read_settings(string ini_path)
        {
            Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in File.ReadAllLines(ini_path))
            {
                int eq = line.IndexOf('=');
                int value;
                if (eq > 0 && Int32.TryParse(line.Substring(eq + 1).Trim(), out value)) { result[line.Substring(0, eq).Trim()] = value; }
            }
            return result;
        }

        public static string read_language(string ini_path)
        {
            foreach (string line in File.ReadAllLines(ini_path))
            {
                int eq = line.IndexOf('=');
                if (eq > 0 && line.Substring(0, eq).Trim().Equals("LanguageFile", StringComparison.OrdinalIgnoreCase))
                {
                    string file = Path.GetFileNameWithoutExtension(line.Substring(eq + 1).Trim());
                    return file.Length > 0 ? file : null;
                }
            }
            return null;
        }

        private static string wide(byte[] data, int offset, int length)
        {
            return Encoding.Unicode.GetString(data, offset, length).Split('\0')[0].Trim();
        }
    }
}
