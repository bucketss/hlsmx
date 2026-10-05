using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace hlsmx
{
    static class CfImport
    {
        public const string options_file = "options_data.txt";
        public const string servers_file = "servers_data.txt";
        private const int field_count = 15;
        private static readonly string[] priorities_cn = { "低", "低于正常", "正常", "高于正常", "高", "实时" };

        private static IEnumerable<string> content(string[] lines)
        {
            return lines.Where(l => l.Trim().Length > 0);
        }

        public static bool is_servers(string[] lines)
        {
            List<string> rows = content(lines).ToList();
            return rows.Count > 0 && rows.All(l => l.Split('`').Length >= field_count);
        }

        public static bool is_options(string[] lines)
        {
            List<string> rows = content(lines).ToList();
            int value;
            return rows.Count >= 4 && rows.Count <= 5 && rows.All(l => Int32.TryParse(l.Trim(), out value));
        }

        private static string priority(string value)
        {
            int index = Array.IndexOf(priorities_cn, value);
            if (index >= 0) { return ServerEditForm.Priorities[index]; }
            return ServerEditForm.Priorities.Contains(value) ? value : "Normal";
        }

        public static List<ServerEntry> read_servers(string path)
        {
            string[] lines = File.ReadAllLines(path);
            if (!is_servers(lines)) { throw new InvalidDataException("Not a CF Server Monitor server list."); }
            List<ServerEntry> result = new List<ServerEntry>();
            foreach (string line in content(lines))
            {
                string[] f = line.Split('`');
                ServerEntry s = new ServerEntry();
                s.Name = f[0];
                s.IP = f[1].Trim().Length > 0 ? f[1].Trim() : "127.0.0.1";
                s.Port = f[2];
                s.Executable = f[3];
                s.Priority = priority(f[4]);
                s.Paused = true;
                Int32.TryParse(f[9], out s.Restarts);
                s.HideConsole = f[12] == "隐藏" || f[12] == "Hidden";
                s.Cores = f[13];
                s.Params = f[14];
                result.Add(s);
            }
            return result;
        }

        public static int[] read_settings(string path)
        {
            string[] lines = File.ReadAllLines(path);
            if (!is_options(lines)) { throw new InvalidDataException("Not a CF Server Monitor options file."); }
            return content(lines).Select(l => Int32.Parse(l.Trim())).ToArray();
        }
    }
}
