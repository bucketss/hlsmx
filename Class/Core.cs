using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;

namespace hlsmx
{
    class Core
    {
        private static readonly Lazy<Core> shared = new Lazy<Core>(() => new Core());
        public static Core Instance { get { return shared.Value; } }
        private Core() { }

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hWnd);
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int pid);
        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder name, int max);

        public static bool show_window(long hwnd, bool show)
        {
            return hwnd != 0 && ShowWindow((IntPtr)hwnd, show ? 5 : 0);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool SetWindowText(IntPtr hWnd, string text);

        public static void set_title(long hwnd, string title)
        {
            if (hwnd == 0 || string.IsNullOrEmpty(title)) { return; }
            StringBuilder current = new StringBuilder(512);
            GetWindowText((IntPtr)hwnd, current, current.Capacity);
            if (current.ToString() != title) { SetWindowText((IntPtr)hwnd, title); }
        }

        public static bool window_of(long hwnd, int pid)
        {
            int owner;
            return hwnd != 0 && pid > 0 && IsWindow((IntPtr)hwnd) && GetWindowThreadProcessId((IntPtr)hwnd, out owner) != 0 && owner == pid;
        }

        public static long find_window(int pid)
        {
            if (pid < 1) { return 0; }
            IntPtr console = IntPtr.Zero, visible = IntPtr.Zero, hidden = IntPtr.Zero;
            EnumWindows((hwnd, l) =>
            {
                int owner;
                GetWindowThreadProcessId(hwnd, out owner);
                if (owner != pid || GetWindow(hwnd, 4) != IntPtr.Zero) { return true; }
                StringBuilder name = new StringBuilder(64);
                GetClassName(hwnd, name, name.Capacity);
                string cls = name.ToString();
                if (cls == "ConsoleWindowClass") { console = hwnd; return false; }
                if (cls == "IME" || cls == "MSCTFIME UI" || cls.StartsWith("GDI+")) { return true; }
                if (IsWindowVisible(hwnd)) { if (visible == IntPtr.Zero) { visible = hwnd; } }
                else if (hidden == IntPtr.Zero && GetWindowTextLength(hwnd) > 0) { hidden = hwnd; }
                return true;
            }, IntPtr.Zero);
            IntPtr found = console != IntPtr.Zero ? console : (visible != IntPtr.Zero ? visible : hidden);
            return found.ToInt64();
        }

        public Action<string> log_sink;

        public static readonly string base_dir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string appdata_dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "hlsmx");
        private static readonly string location_file = Path.Combine(base_dir, "data_location.txt");
        private static readonly string[] data_files = { "settings.json", "servers.json", "status.json" };
        public static string data_dir = read_location();
        public static string status_path { get { return Path.Combine(data_dir, "status.json"); } }
        public static string logs_dir { get { return Path.Combine(data_dir, "logs"); } }
        private string settings_path { get { return Path.Combine(data_dir, "settings.json"); } }
        private string servers_path { get { return Path.Combine(data_dir, "servers.json"); } }
        public static bool data_in_appdata { get { return !same_dir(data_dir, base_dir); } }

        private static string read_location()
        {
            try
            {
                if (File.Exists(location_file))
                {
                    string dir = File.ReadAllText(location_file).Trim();
                    if (dir.Length > 0 && Directory.Exists(dir)) { return dir; }
                }
            }
            catch { }
            return base_dir;
        }
        private static bool same_dir(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }
        public bool move_data(bool appdata)
        {
            string target = appdata ? appdata_dir : base_dir;
            if (same_dir(target, data_dir)) { return false; }
            lock (save_lock)
            {
                lock (log_lock)
                {
                    Directory.CreateDirectory(target);
                    foreach (string name in data_files)
                    {
                        string from = Path.Combine(data_dir, name);
                        if (File.Exists(from)) { File.Copy(from, Path.Combine(target, name), true); }
                    }
                    string logs_from = logs_dir;
                    string logs_to = Path.Combine(target, "logs");
                    if (Directory.Exists(logs_from))
                    {
                        Directory.CreateDirectory(logs_to);
                        foreach (string from in Directory.GetFiles(logs_from))
                        {
                            string to = Path.Combine(logs_to, Path.GetFileName(from));
                            if (File.Exists(to)) { File.AppendAllText(to, File.ReadAllText(from)); } else { File.Copy(from, to); }
                        }
                    }
                    if (appdata) { File.WriteAllText(location_file, target); } else if (File.Exists(location_file)) { File.Delete(location_file); }
                    string old = data_dir;
                    data_dir = target;
                    foreach (string name in data_files) { try { File.Delete(Path.Combine(old, name)); } catch { } }
                    if (Directory.Exists(logs_from)) { try { Directory.Delete(logs_from, true); } catch { } }
                }
            }
            return true;
        }
        private string hlsm_ini = Path.Combine(base_dir, "hlsm.ini");
        private string hlsm_list = Path.Combine(base_dir, "hlsm.l24");
        private Settings settings = new Settings();

        private static int clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
        private static string set_aside(string path)
        {
            string broken = path + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".broken";
            try { File.Move(path, broken); } catch { }
            return broken;
        }
        public string peek_language()
        {
            try
            {
                Settings s = File.Exists(settings_path) ? Json.Load<Settings>(settings_path) : null;
                return s != null && s.Language != null ? s.Language : "";
            }
            catch { return ""; }
        }
        public string ReadConfig()
        {
            string message_key = null;
            object[] message_args = new object[] { };
            bool save = true;
            string legacy = Path.Combine(base_dir, CfImport.options_file);
            if (File.Exists(settings_path))
            {
                try { settings = Json.Load<Settings>(settings_path) ?? new Settings(); }
                catch { settings = new Settings(); save = false; message_key = "log.settings_broken"; message_args = new object[] { Path.GetFileName(set_aside(settings_path)) }; }
            }
            else if (File.Exists(legacy))
            {
                try
                {
                    int[] values = CfImport.read_settings(legacy);
                    settings.CheckInterval = values[0];
                    settings.ProcessCheckRetries = values[1];
                    settings.NetworkCheckRetries = values[2];
                    settings.MaxSimultaneousRestarts = values[3];
                    if (values.Length > 4) { settings.CloseToTray = values[4] != 0; }
                    message_key = "log.cf_options_ok";
                }
                catch (Exception e) { message_key = "log.cf_options_failed"; message_args = new object[] { e.Message }; }
            }
            else if (File.Exists(hlsm_ini))
            {
                try
                {
                    Dictionary<string, int> values = HlsmImport.read_settings(hlsm_ini);
                    int v;
                    if (values.TryGetValue("CheckInterval", out v)) { settings.CheckInterval = v; }
                    if (values.TryGetValue("ProcessCheckTimes", out v)) { settings.ProcessCheckRetries = v; }
                    if (values.TryGetValue("NetworkCheckTimes", out v)) { settings.NetworkCheckRetries = v; }
                    if (values.TryGetValue("MaxStartCount", out v)) { settings.MaxSimultaneousRestarts = v; }
                    string language = HlsmImport.read_language(hlsm_ini);
                    if (language != null && File.Exists(Path.Combine(Lang.dir, language + ".ini"))) { settings.Language = language; }
                    message_key = "log.hlsm_ini_ok";
                }
                catch (Exception e) { message_key = "log.hlsm_ini_failed"; message_args = new object[] { e.Message }; }
            }
            normalize();
            Lang.load(Lang.resolve(settings.Language));
            if (save) { SaveConfig(); }
            return message_key == null ? null : Lang.F(message_key, message_args);
        }
        public void normalize()
        {
            settings.CheckInterval = clamp(settings.CheckInterval, 2, 30);
            settings.ProcessCheckRetries = clamp(settings.ProcessCheckRetries, 1, 30);
            settings.NetworkCheckRetries = clamp(settings.NetworkCheckRetries, 2, 60);
            settings.MaxSimultaneousRestarts = clamp(settings.MaxSimultaneousRestarts, 0, 64);
            settings.CrashLoopRestarts = clamp(settings.CrashLoopRestarts, 0, 100);
            settings.ShutdownTimeout = clamp(settings.ShutdownTimeout, 0, 120);
            if (settings.Icon == null) { settings.Icon = ""; }
            if (settings.Icon.Equals(default_icon, StringComparison.OrdinalIgnoreCase)) { settings.Icon = ""; }
            if (settings.Icon.StartsWith("lombda", StringComparison.OrdinalIgnoreCase) && !File.Exists(icon_path(settings.Icon)) && File.Exists(icon_path("hlsmx" + settings.Icon.Substring(6)))) { settings.Icon = "hlsmx" + settings.Icon.Substring(6); }
            if (Array.IndexOf(Theme.Modes, settings.Theme) < 0) { settings.Theme = "System"; }
            if (settings.WebhookUrl == null) { settings.WebhookUrl = ""; }
            if (settings.Webhooks == null) { settings.Webhooks = new List<WebhookSetting>(); }
            if (settings.WebhookUrl.Trim().Length > 0)
            {
                WebhookSetting migrated = new WebhookSetting();
                migrated.Url = settings.WebhookUrl.Trim();
                settings.Webhooks.Insert(0, migrated);
                settings.WebhookUrl = "";
            }
            settings.Webhooks = settings.Webhooks.Where(h => h != null && !string.IsNullOrEmpty(h.Url)).ToList();
            foreach (WebhookSetting h in settings.Webhooks)
            {
                if (h.Name == null) { h.Name = ""; }
                if (Array.IndexOf(WebhookTypes, h.Type) < 0) { h.Type = ""; }
            }
            if (settings.Language == null) { settings.Language = ""; }
        }
        private readonly object save_lock = new object();
        public void SaveConfig()
        {
            lock (save_lock) { Json.Save(settings_path, settings); }
        }
        public int opt_check_interval { get { return settings.CheckInterval; } set { settings.CheckInterval = value; } }
        public int opt_process_retries { get { return settings.ProcessCheckRetries; } set { settings.ProcessCheckRetries = value; } }
        public int opt_network_retries { get { return settings.NetworkCheckRetries; } set { settings.NetworkCheckRetries = value; } }
        public int opt_max_restarts { get { return settings.MaxSimultaneousRestarts; } set { settings.MaxSimultaneousRestarts = value; } }
        public bool opt_tray { get { return settings.CloseToTray; } set { settings.CloseToTray = value; } }
        public string opt_language { get { return settings.Language; } set { settings.Language = value ?? ""; } }
        public bool opt_list_local_ips { get { return settings.ListLocalIps; } set { settings.ListLocalIps = value; } }
        public int opt_loop_count { get { return settings.CrashLoopRestarts; } set { settings.CrashLoopRestarts = value; } }
        public string opt_icon { get { return settings.Icon; } set { settings.Icon = value ?? ""; } }
        public string opt_theme { get { return settings.Theme; } set { settings.Theme = value; } }
        public int opt_shutdown { get { return settings.ShutdownTimeout; } set { settings.ShutdownTimeout = value; } }
        public bool opt_exclude_bots { get { return settings.ExcludeBots; } set { settings.ExcludeBots = value; } }
        public bool opt_exclude_hltv { get { return settings.ExcludeHltv; } set { settings.ExcludeHltv = value; } }
        public bool opt_wait_empty { get { return settings.WaitForEmpty; } set { settings.WaitForEmpty = value; } }
        public bool opt_warn_rcon { get { return settings.WarnRcon; } set { settings.WarnRcon = value; } }
        public bool opt_skip_busy { get { return settings.SkipBusy; } set { settings.SkipBusy = value; } }
        public List<WebhookSetting> opt_webhooks
        {
            get { return settings.Webhooks.Select(h => h.copy()).ToList(); }
            set { settings.Webhooks = (value ?? new List<WebhookSetting>()).Where(h => h != null && !string.IsNullOrEmpty(h.Url)).Select(h => h.copy()).ToList(); }
        }
        public Settings layout { get { return settings; } }

        public const string NOTIFY_START = "start";
        public const string NOTIFY_RESTART = "restart";
        public const string NOTIFY_CRASH_LOOP = "crash_loop";
        public const string NOTIFY_SCHEDULE = "schedule";

        public void notify(string kind, string text)
        {
            foreach (WebhookSetting hook in settings.Webhooks.ToList())
            {
                if (!hook.sends(kind) || string.IsNullOrEmpty(hook.Url)) { continue; }
                WebhookSetting target = hook;
                string url = hook.Url;
                string type = hook.Type;
                string label = hook.Name.Length > 0 ? hook.Name : url;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    string resolved;
                    string error = post_webhook(url, type, text, out resolved);
                    Action<string> sink = log_sink;
                    if (error != null && sink != null) { sink(Lang.F("log.webhook_failed", label, error)); }
                    if (resolved == null || has_scheme(url)) { return; }
                    lock (save_lock)
                    {
                        if (target.Url != url) { return; }
                        target.Url = resolved;
                    }
                    try { SaveConfig(); } catch { }
                });
            }
        }

        public static string webhook_kind_name(string url, string type)
        {
            string kind = webhook_kind(url, type);
            if (kind == null) { return Lang.T("webhook.bad_url"); }
            return kind == WEBHOOK_GENERIC ? Lang.T("webhook.kind_generic") : kind;
        }

        public const string WEBHOOK_DISCORD = "Discord";
        public const string WEBHOOK_SLACK = "Slack";
        public const string WEBHOOK_NTFY = "ntfy";
        public const string WEBHOOK_GENERIC = "";
        public const string WEBHOOK_TYPE_GENERIC = "Generic";
        public static readonly string[] WebhookTypes = { "", WEBHOOK_DISCORD, WEBHOOK_SLACK, WEBHOOK_NTFY, WEBHOOK_TYPE_GENERIC };

        public static bool has_scheme(string url)
        {
            return (url ?? "").Contains("://");
        }

        public static Uri webhook_uri(string url)
        {
            Uri uri;
            if (string.IsNullOrEmpty(url)) { return null; }
            url = url.Trim();
            if (!has_scheme(url)) { url = "https://" + url; }
            return Uri.TryCreate(url, UriKind.Absolute, out uri) ? uri : null;
        }

        public static string webhook_kind(string url, string type)
        {
            Uri uri = webhook_uri(url);
            if (uri == null || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) { return null; }
            if (type == WEBHOOK_TYPE_GENERIC) { return WEBHOOK_GENERIC; }
            if (!string.IsNullOrEmpty(type) && Array.IndexOf(WebhookTypes, type) >= 0) { return type; }
            string host = uri.Host.ToLowerInvariant();
            string path = uri.AbsolutePath.ToLowerInvariant();
            if ((host == "discord.com" || host.EndsWith(".discord.com") || host == "discordapp.com" || host.EndsWith(".discordapp.com")) && path.StartsWith("/api/webhooks/")) { return WEBHOOK_DISCORD; }
            if (host == "hooks.slack.com") { return WEBHOOK_SLACK; }
            if (host.Contains("ntfy")) { return WEBHOOK_NTFY; }
            return WEBHOOK_GENERIC;
        }

        [DataContract]
        class DiscordMessage
        {
            [DataMember] public string username;
            [DataMember] public string content;
        }

        [DataContract]
        class SlackMessage
        {
            [DataMember] public string text;
        }

        [DataContract]
        class GenericMessage
        {
            [DataMember(Order = 0)] public string source;
            [DataMember(Order = 1)] public string machine;
            [DataMember(Order = 2)] public string time;
            [DataMember(Order = 3)] public string text;
        }

        private static byte[] to_json(object message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(message.GetType()).WriteObject(ms, message);
                return ms.ToArray();
            }
        }

        public string post_webhook(string url, string type, string text, out string resolved)
        {
            resolved = null;
            string kind = webhook_kind(url, type);
            if (kind == null) { return Lang.T("webhook.bad_url"); }
            url = url.Trim();
            bool reached;
            if (has_scheme(url))
            {
                resolved = url;
                return send_webhook(url, kind, text, out reached);
            }
            string error = send_webhook("https://" + url, kind, text, out reached);
            if (reached) { resolved = "https://" + url; return error; }
            string fallback = send_webhook("http://" + url, kind, text, out reached);
            if (reached) { resolved = "http://" + url; return fallback; }
            return error;
        }

        private string send_webhook(string url, string kind, string text, out bool reached)
        {
            reached = false;
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                byte[] body;
                string content_type = "application/json";
                if (kind == WEBHOOK_DISCORD)
                {
                    DiscordMessage message = new DiscordMessage();
                    message.username = "HLSMX";
                    message.content = text.Length > 1900 ? text.Substring(0, 1900) : text;
                    body = to_json(message);
                }
                else if (kind == WEBHOOK_SLACK)
                {
                    SlackMessage message = new SlackMessage();
                    message.text = text;
                    body = to_json(message);
                }
                else if (kind == WEBHOOK_NTFY)
                {
                    body = Encoding.UTF8.GetBytes(text);
                    content_type = "text/plain; charset=utf-8";
                }
                else
                {
                    GenericMessage message = new GenericMessage();
                    message.source = "HLSMX";
                    message.machine = Environment.MachineName;
                    message.time = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
                    message.text = text;
                    body = to_json(message);
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url.Trim());
                request.Method = "POST";
                request.ContentType = content_type;
                if (kind == WEBHOOK_NTFY) { request.Headers["Title"] = "HLSMX"; }
                request.Timeout = 10000;
                request.ContentLength = body.Length;
                using (Stream s = request.GetRequestStream()) { s.Write(body, 0, body.Length); }
                using (request.GetResponse()) { }
                reached = true;
                return null;
            }
            catch (WebException e)
            {
                reached = e.Status == WebExceptionStatus.ProtocolError;
                return e.Message;
            }
            catch (Exception e) { return e.Message; }
        }

        public void SaveConfig_Servers(List<ServerEntry> servers)
        {
            ServerList list = new ServerList();
            list.Servers = servers;
            Json.Save(servers_path, list);
        }
        public List<ServerEntry> ReadConfig_Servers(out string error)
        {
            error = null;
            string legacy = Path.Combine(base_dir, CfImport.servers_file);
            if (File.Exists(servers_path))
            {
                try
                {
                    ServerList list = Json.Load<ServerList>(servers_path);
                    List<ServerEntry> servers = (list == null || list.Servers == null) ? new List<ServerEntry>() : list.Servers;
                    foreach (ServerEntry s in servers) { if (s.Schedules == null) { s.Schedules = new List<ScheduleEntry>(); } }
                    return servers;
                }
                catch
                {
                    error = Lang.F("log.servers_broken", Path.GetFileName(set_aside(servers_path)));
                    return new List<ServerEntry>();
                }
            }
            List<ServerEntry> result = new List<ServerEntry>();
            if (File.Exists(legacy))
            {
                try
                {
                    result = CfImport.read_servers(legacy);
                    SaveConfig_Servers(result);
                    error = Lang.F("log.cf_list_ok", result.Count);
                }
                catch (Exception e) { error = Lang.F("log.cf_list_failed", e.Message); }
            }
            else if (File.Exists(hlsm_list))
            {
                try
                {
                    result = HlsmImport.read_servers(hlsm_list);
                    SaveConfig_Servers(result);
                    error = Lang.F("log.hlsm_list_ok", result.Count);
                }
                catch (Exception e) { error = Lang.F("log.hlsm_list_failed", e.Message); }
            }
            return result;
        }

        private readonly object log_lock = new object();
        public void write_log(string line)
        {
            try
            {
                lock (log_lock)
                {
                    string dir = logs_dir;
                    Directory.CreateDirectory(dir);
                    File.AppendAllText(Path.Combine(dir, DateTime.Now.ToString("yyyy-MM") + ".log"), line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        private const int max_icon_files = 1024;
        private const int max_icon_scan = 2048;
        private const long max_icon_bytes = 1024 * 1024;
        private const int max_icon_pixels = 1024;
        private static readonly int[] png_icon_sizes = { 16, 24, 32, 48, 64, 128, 256 };
        public const string default_icon = "hlsmx.png";

        public string[] icon_list()
        {
            string dir = Path.Combine(base_dir, "icons");
            if (!Directory.Exists(dir)) { return new string[] { }; }
            try
            {
                return Directory.EnumerateFiles(dir).Take(max_icon_scan).Where(valid_icon_file).Take(max_icon_files)
                    .Select(f => Path.GetFileName(f)).Where(f => !f.Equals(default_icon, StringComparison.OrdinalIgnoreCase)).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch { return new string[] { }; }
        }
        private static bool is_png(string name) { return name.EndsWith(".png", StringComparison.OrdinalIgnoreCase); }
        private static bool valid_icon_file(string path)
        {
            bool png = is_png(path);
            if (!png && !path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) { return false; }
            try
            {
                FileInfo info = new FileInfo(path);
                if (info.Length < 24 || info.Length > max_icon_bytes) { return false; }
                byte[] head = new byte[24];
                using (FileStream fs = File.OpenRead(path)) { if (fs.Read(head, 0, 24) < 24) { return false; } }
                if (png)
                {
                    if (!(head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 && head[4] == 0x0D && head[5] == 0x0A && head[6] == 0x1A && head[7] == 0x0A)) { return false; }
                    int w = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                    int h = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
                    return w > 0 && h > 0 && w <= max_icon_pixels && h <= max_icon_pixels;
                }
                return head[0] == 0 && head[1] == 0 && head[2] == 1 && head[3] == 0 && (head[4] | head[5]) != 0;
            }
            catch { return false; }
        }
        private static string icon_path(string name) { return Path.Combine(Path.Combine(base_dir, "icons"), Path.GetFileName(name)); }
        private static byte[] read_icon_bytes(string name)
        {
            string path = icon_path(name);
            if (!valid_icon_file(path)) { return null; }
            return File.ReadAllBytes(path);
        }
        private static Bitmap trim(Image src)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp)) { g.DrawImage(src, 0, 0, src.Width, src.Height); }
            System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int[] pixels = new int[bmp.Width * bmp.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            bmp.UnlockBits(data);
            int left = bmp.Width, top = bmp.Height, right = -1, bottom = -1;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    if (((pixels[y * bmp.Width + x] >> 24) & 0xFF) == 0) { continue; }
                    if (x < left) { left = x; }
                    if (x > right) { right = x; }
                    if (y < top) { top = y; }
                    if (y > bottom) { bottom = y; }
                }
            }
            if (right < 0 || (left == 0 && top == 0 && right == bmp.Width - 1 && bottom == bmp.Height - 1)) { return bmp; }
            Bitmap cropped = bmp.Clone(new Rectangle(left, top, right - left + 1, bottom - top + 1), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bmp.Dispose();
            return cropped;
        }
        private static Bitmap fit_square(Image src, int size)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
            int w = Math.Max(1, (int)Math.Round(src.Width * scale)), h = Math.Max(1, (int)Math.Round(src.Height * scale));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(src, (size - w) / 2, (size - h) / 2, w, h);
            }
            return bmp;
        }
        private static Icon icon_from_png(byte[] png)
        {
            List<byte[]> frames = new List<byte[]>();
            using (MemoryStream src_stream = new MemoryStream(png))
            using (Image original = Image.FromStream(src_stream))
            {
                if (original.Width > max_icon_pixels || original.Height > max_icon_pixels) { throw new InvalidDataException(); }
                using (Bitmap src = trim(original))
                foreach (int size in png_icon_sizes)
                {
                    using (Bitmap bmp = fit_square(src, size))
                    using (MemoryStream frame = new MemoryStream())
                    {
                        bmp.Save(frame, System.Drawing.Imaging.ImageFormat.Png);
                        frames.Add(frame.ToArray());
                    }
                }
            }
            MemoryStream ico = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ico);
            bw.Write((ushort)0); bw.Write((ushort)1); bw.Write((ushort)frames.Count);
            int offset = 6 + 16 * frames.Count;
            for (int i = 0; i < frames.Count; i++)
            {
                int size = png_icon_sizes[i];
                bw.Write((byte)(size >= 256 ? 0 : size)); bw.Write((byte)(size >= 256 ? 0 : size));
                bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((ushort)1); bw.Write((ushort)32);
                bw.Write(frames[i].Length); bw.Write(offset);
                offset += frames[i].Length;
            }
            foreach (byte[] f in frames) { bw.Write(f); }
            bw.Flush();
            ico.Position = 0;
            return new Icon(ico);
        }
        public Icon load_icon(string name)
        {
            if (string.IsNullOrEmpty(name)) { name = default_icon; }
            try
            {
                byte[] data = read_icon_bytes(name);
                if (data != null) { return is_png(name) ? icon_from_png(data) : new Icon(new MemoryStream(data)); }
            }
            catch { }
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        private Icon cached_icon;
        private string cached_icon_name;
        public Icon app_icon
        {
            get
            {
                if (cached_icon == null || cached_icon_name != settings.Icon)
                {
                    cached_icon = load_icon(settings.Icon);
                    cached_icon_name = settings.Icon;
                }
                return cached_icon;
            }
        }
        public Bitmap icon_preview(string name, int size)
        {
            if (string.IsNullOrEmpty(name)) { name = default_icon; }
            try
            {
                byte[] data = read_icon_bytes(name);
                if (data != null && is_png(name))
                {
                    using (MemoryStream ms = new MemoryStream(data))
                    using (Image png = Image.FromStream(ms))
                    {
                        if (png.Width > max_icon_pixels || png.Height > max_icon_pixels) { throw new InvalidDataException(); }
                        using (Bitmap trimmed = trim(png)) { return fit_square(trimmed, size); }
                    }
                }
                int count = BitConverter.ToUInt16(data, 4);
                int best = -1, best_size = 0;
                for (int i = 0; i < count; i++)
                {
                    int w = data[6 + 16 * i] == 0 ? 256 : data[6 + 16 * i];
                    if (best < 0 || (w >= size && (best_size < size || w < best_size)) || (w < size && best_size < size && w > best_size)) { best = i; best_size = w; }
                }
                if (best >= 0)
                {
                    int length = BitConverter.ToInt32(data, 6 + 16 * best + 8);
                    int offset = BitConverter.ToInt32(data, 6 + 16 * best + 12);
                    if (data[offset] == 0x89 && data[offset + 1] == 0x50)
                    {
                        using (MemoryStream ms = new MemoryStream(data, offset, length))
                        using (Image png = Image.FromStream(ms))
                        {
                            return new Bitmap(png, size, size);
                        }
                    }
                }
            }
            catch { }
            using (Icon icon = load_icon(name))
            using (Icon sized = new Icon(icon, size, size))
            {
                return sized.ToBitmap();
            }
        }

        public class ProcInfo
        {
            public int Pid;
            public string Path;
            public long StartTicks;
        }

        public class StartResult
        {
            public int Pid;
            public long StartTicks;
            public long Window;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved, lpDesktop, lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateProcess(string application, StringBuilder command_line, IntPtr process_attributes, IntPtr thread_attributes, bool inherit_handles, uint flags, IntPtr environment, string directory, ref STARTUPINFO startup, out PROCESS_INFORMATION info);
        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll")]
        static extern bool SetProcessAffinityMask(IntPtr process, IntPtr mask);
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int access, bool inherit, int pid);
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);
        [DllImport("kernel32.dll")]
        static extern bool GetProcessTimes(IntPtr process, out long creation, out long exit, out long kernel, out long user);
        [DllImport("kernel32.dll")]
        static extern bool GetExitCodeProcess(IntPtr process, out int code);

        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const int STILL_ACTIVE = 259;
        private const uint CREATE_SUSPENDED = 0x4;
        private const uint CREATE_NEW_CONSOLE = 0x10;

        private static string full_path(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return path ?? ""; }
        }

        public static bool same_path(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) { return false; }
            return string.Equals(full_path(a), full_path(b), StringComparison.OrdinalIgnoreCase);
        }

        private static string exe_name(string path)
        {
            try { return Path.GetFileNameWithoutExtension(path); }
            catch { return null; }
        }

        public static ProcInfo query(int pid)
        {
            if (pid < 1) { return null; }
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (handle == IntPtr.Zero) { return null; }
            try { return query(handle, pid); }
            finally { CloseHandle(handle); }
        }

        private static ProcInfo query(IntPtr handle, int pid)
        {
            int code;
            if (!GetExitCodeProcess(handle, out code) || code != STILL_ACTIVE) { return null; }
            StringBuilder name = new StringBuilder(1024);
            int size = name.Capacity;
            if (!QueryFullProcessImageName(handle, 0, name, ref size)) { return null; }
            long creation, exit, kernel, user;
            if (!GetProcessTimes(handle, out creation, out exit, out kernel, out user)) { return null; }
            ProcInfo info = new ProcInfo();
            info.Pid = pid;
            info.Path = name.ToString();
            info.StartTicks = creation;
            return info;
        }

        public static bool alive(int pid, long start_ticks, string exe)
        {
            ProcInfo info = query(pid);
            return info != null && same_path(info.Path, exe) && (start_ticks == 0 || info.StartTicks == start_ticks);
        }

        public static Dictionary<int, ProcInfo> snapshot(IEnumerable<string> exe_paths)
        {
            HashSet<string> names = new HashSet<string>(exe_paths.Select(exe_name).Where(n => !string.IsNullOrEmpty(n)), StringComparer.OrdinalIgnoreCase);
            Dictionary<int, ProcInfo> result = new Dictionary<int, ProcInfo>();
            if (names.Count == 0) { return result; }
            foreach (Process proc in Process.GetProcesses())
            {
                using (proc)
                {
                    try
                    {
                        if (!names.Contains(proc.ProcessName)) { continue; }
                        ProcInfo info = query(proc.Id);
                        if (info != null) { result[info.Pid] = info; }
                    }
                    catch { }
                }
            }
            return result;
        }

        public static ProcessPriorityClass priority_class(string priority)
        {
            switch (priority)
            {
                case "Low": return ProcessPriorityClass.Idle;
                case "Below Normal": return ProcessPriorityClass.BelowNormal;
                case "Above Normal": return ProcessPriorityClass.AboveNormal;
                case "High": return ProcessPriorityClass.High;
                case "Realtime": return ProcessPriorityClass.RealTime;
            }
            return ProcessPriorityClass.Normal;
        }

        public static long affinity_mask(string cores)
        {
            int count = Math.Min(Environment.ProcessorCount, 64);
            long mask = 0;
            foreach (string s in (cores ?? "").Split('|'))
            {
                int core;
                if (Int32.TryParse(s, out core) && core >= 0 && core < count) { mask |= 1L << core; }
            }
            return mask;
        }

        private static long all_cores_mask()
        {
            int count = Math.Min(Environment.ProcessorCount, 64);
            return count >= 64 ? -1L : (1L << count) - 1;
        }

        public StartResult start_process(string path, string param, string priority, string cores, bool show, string title)
        {
            StartResult result = new StartResult();
            if (!File.Exists(path)) { return result; }
            STARTUPINFO startup = new STARTUPINFO();
            startup.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            if (!string.IsNullOrEmpty(title)) { startup.lpTitle = title; }
            if (!show) { startup.dwFlags = 0x1; startup.wShowWindow = 0; }
            StringBuilder command = new StringBuilder("\"" + path + "\"" + (string.IsNullOrEmpty(param) ? "" : " " + param));
            uint flags = CREATE_SUSPENDED | CREATE_NEW_CONSOLE | (uint)priority_class(priority);
            PROCESS_INFORMATION info;
            if (!CreateProcess(path, command, IntPtr.Zero, IntPtr.Zero, false, flags, IntPtr.Zero, Path.GetDirectoryName(path), ref startup, out info)) { return result; }
            try
            {
                long mask = affinity_mask(cores);
                if (mask != 0) { try { SetProcessAffinityMask(info.hProcess, (IntPtr)mask); } catch { } }
                ResumeThread(info.hThread);
                ProcInfo started = query(info.hProcess, info.dwProcessId);
                result.Pid = info.dwProcessId;
                result.StartTicks = started != null ? started.StartTicks : 0;
            }
            finally
            {
                CloseHandle(info.hThread);
                CloseHandle(info.hProcess);
            }
            for (int i = 0; i < 40 && result.Window == 0; i++)
            {
                result.Window = find_window(result.Pid);
                if (result.Window == 0) { Thread.Sleep(250); }
            }
            if (!show) { show_window(result.Window, false); }
            return result;
        }

        public void shutdown_process(int pid, long start_ticks, string exe, string ip, int port, string password, int timeout)
        {
            if (!alive(pid, start_ticks, exe)) { return; }
            try
            {
                using (Process proc = Process.GetProcessById(pid))
                {
                    if (timeout > 0 && Rcon.send(ip, port, password, "quit") && proc.WaitForExit(timeout * 1000)) { return; }
                    proc.Kill();
                    proc.WaitForExit(5000);
                }
            }
            catch { }
        }

        public bool process_priority(int pid, string priority)
        {
            if (pid < 1) { return false; }
            try
            {
                using (Process proc = Process.GetProcessById(pid)) { proc.PriorityClass = priority_class(priority); }
                return true;
            }
            catch { return false; }
        }

        public bool process_affinity(int pid, string cores)
        {
            if (pid < 1) { return false; }
            long mask = affinity_mask(cores);
            if (mask == 0) { mask = all_cores_mask(); }
            try
            {
                using (Process proc = Process.GetProcessById(pid)) { proc.ProcessorAffinity = (IntPtr)mask; }
                return true;
            }
            catch { return false; }
        }
    }
}
