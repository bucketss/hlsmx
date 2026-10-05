using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace hlsmx
{
    static class Lang
    {
        public class Info
        {
            public string Id;
            public string Name;
            public string[] Cultures;
        }

        public const string Fallback = "english";
        private const int max_files = 100;
        private const long max_bytes = 512 * 1024;

        private static Dictionary<string, string> strings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static string Current = Fallback;

        public static string dir { get { return Path.Combine(Core.base_dir, "lang"); } }

        public static string T(string key)
        {
            string value;
            if (strings.TryGetValue(key, out value)) { return value; }
            if (LangDefaults.Strings.TryGetValue(key, out value)) { return value; }
            return key;
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); }
            catch (FormatException)
            {
                string value;
                return LangDefaults.Strings.TryGetValue(key, out value) ? string.Format(value, args) : key;
            }
        }

        public static string priority(string value)
        {
            string key = "priority." + (value ?? "").ToLowerInvariant().Replace(' ', '_');
            string text = T(key);
            return text == key ? value : text;
        }

        public static string service_status(string status)
        {
            string key = status == "Not installed" ? "svc.not_installed" : "svc." + (status ?? "").ToLowerInvariant();
            string text = T(key);
            return text == key ? status : text;
        }

        public static List<Info> available()
        {
            List<Info> result = new List<Info>();
            try
            {
                if (!Directory.Exists(dir)) { return result; }
                foreach (string path in Directory.EnumerateFiles(dir, "*.ini").Take(max_files))
                {
                    Dictionary<string, Dictionary<string, string>> sections = parse(path);
                    if (sections == null) { continue; }
                    Dictionary<string, string> header;
                    sections.TryGetValue("Language", out header);
                    Info info = new Info();
                    info.Id = Path.GetFileNameWithoutExtension(path);
                    string name, cultures;
                    info.Name = header != null && header.TryGetValue("Name", out name) && name.Length > 0 ? name : info.Id;
                    info.Cultures = header != null && header.TryGetValue("Cultures", out cultures) ? cultures.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries) : new string[] { };
                    result.Add(info);
                }
            }
            catch { }
            return result.OrderBy(i => i.Id == Fallback ? 0 : 1).ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public static string resolve(string setting)
        {
            List<Info> langs = available();
            if (!string.IsNullOrEmpty(setting))
            {
                Info chosen = langs.FirstOrDefault(l => l.Id.Equals(setting, StringComparison.OrdinalIgnoreCase));
                if (chosen != null) { return chosen.Id; }
            }
            CultureInfo ui = CultureInfo.CurrentUICulture;
            Info match = langs.FirstOrDefault(l => l.Cultures.Any(c => c.Equals(ui.Name, StringComparison.OrdinalIgnoreCase)))
                ?? langs.FirstOrDefault(l => l.Cultures.Any(c => c.Split('-')[0].Equals(ui.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)));
            return match != null ? match.Id : Fallback;
        }

        public static void load(string id)
        {
            Dictionary<string, string> loaded = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            id = Path.GetFileName(id ?? "");
            if (id.Length > 0)
            {
                Dictionary<string, Dictionary<string, string>> sections = parse(Path.Combine(dir, id + ".ini"));
                Dictionary<string, string> found;
                if (sections != null && sections.TryGetValue("Strings", out found)) { loaded = found; }
            }
            strings = loaded;
            Current = id.Length > 0 ? id : Fallback;
        }

        private static Dictionary<string, Dictionary<string, string>> parse(string path)
        {
            try
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists || info.Length > max_bytes) { return null; }
                Dictionary<string, Dictionary<string, string>> sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> current = null;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) { continue; }
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        string name = line.Substring(1, line.Length - 2).Trim();
                        if (!sections.TryGetValue(name, out current))
                        {
                            current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            sections[name] = current;
                        }
                        continue;
                    }
                    int eq = line.IndexOf('=');
                    if (eq <= 0 || current == null) { continue; }
                    current[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                }
                return sections;
            }
            catch { return null; }
        }
    }
}
