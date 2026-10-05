using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace hlsmx
{
    [DataContract]
    public class Settings
    {
        public Settings() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            CheckInterval = 5;
            ProcessCheckRetries = 3;
            NetworkCheckRetries = 15;
            MaxSimultaneousRestarts = 0;
            CloseToTray = true;
            CrashLoopRestarts = 5;
            Icon = "";
            Theme = "System";
            ShutdownTimeout = 10;
            WebhookUrl = "";
            Language = "";
            Webhooks = new List<WebhookSetting>();
            ExcludeBots = false;
            ExcludeHltv = false;
            WaitForEmpty = false;
            WarnRcon = false;
            SkipBusy = false;
            StartMinimized = false;
        }

        [DataMember(Order = 0)] public int CheckInterval;
        [DataMember(Order = 1)] public int ProcessCheckRetries;
        [DataMember(Order = 2)] public int NetworkCheckRetries;
        [DataMember(Order = 3)] public int MaxSimultaneousRestarts;
        [DataMember(Order = 4)] public bool CloseToTray;
        [DataMember(Order = 5)] public int CrashLoopRestarts;
        [DataMember(Order = 7)] public string Icon;
        [DataMember(Order = 8)] public string Theme;
        [DataMember(Order = 9)] public int ShutdownTimeout;
        [DataMember(Order = 10)] public string WebhookUrl;
        [DataMember(Order = 11)] public int[] WindowBounds;
        [DataMember(Order = 12)] public bool WindowMaximized;
        [DataMember(Order = 13)] public List<ColumnSetting> Columns;
        [DataMember(Order = 14)] public bool ListLocalIps;
        [DataMember(Order = 15)] public string Language;
        [DataMember(Order = 16)] public List<WebhookSetting> Webhooks;
        [DataMember(Order = 17)] public bool ExcludeBots;
        [DataMember(Order = 18)] public bool ExcludeHltv;
        [DataMember(Order = 21)] public bool WaitForEmpty;
        [DataMember(Order = 22)] public bool WarnRcon;
        [DataMember(Order = 23)] public bool SkipBusy;
        [DataMember(Order = 24)] public bool StartMinimized;
    }

    [DataContract]
    public class WebhookSetting
    {
        public WebhookSetting() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            Name = "";
            Url = "";
            NotifyStart = true;
            NotifyRestart = true;
            NotifyCrashLoop = true;
            NotifySchedule = true;
            Type = "";
        }

        [DataMember(Order = 0)] public string Name;
        [DataMember(Order = 1)] public string Url;
        [DataMember(Order = 2)] public bool NotifyStart;
        [DataMember(Order = 3)] public bool NotifyRestart;
        [DataMember(Order = 4)] public bool NotifyCrashLoop;
        [DataMember(Order = 5)] public bool NotifySchedule;
        [DataMember(Order = 6)] public string Type;

        public bool sends(string kind)
        {
            switch (kind)
            {
                case Core.NOTIFY_START: return NotifyStart;
                case Core.NOTIFY_RESTART: return NotifyRestart;
                case Core.NOTIFY_CRASH_LOOP: return NotifyCrashLoop;
                case Core.NOTIFY_SCHEDULE: return NotifySchedule;
            }
            return true;
        }

        public WebhookSetting copy()
        {
            WebhookSetting c = new WebhookSetting();
            c.Name = Name; c.Url = Url;
            c.NotifyStart = NotifyStart; c.NotifyRestart = NotifyRestart; c.NotifyCrashLoop = NotifyCrashLoop; c.NotifySchedule = NotifySchedule;
            c.Type = Type;
            return c;
        }
    }

    [DataContract]
    public class ColumnSetting
    {
        public ColumnSetting() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            Visible = true;
        }

        [DataMember(Order = 0)] public int Index;
        [DataMember(Order = 1)] public int Width;
        [DataMember(Order = 2)] public int DisplayIndex;
        [DataMember(Order = 3)] public bool Visible;
    }

    [DataContract]
    public class ServerEntry
    {
        public ServerEntry() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            Name = "";
            IP = "127.0.0.1";
            Port = "";
            Executable = "";
            Priority = "Normal";
            Paused = true;
            Restarts = 0;
            HideConsole = false;
            Cores = "";
            Params = "";
            Schedules = new List<ScheduleEntry>();
            RconPassword = "";
            LastRestart = "";
            Color = "";
            Pid = 0;
            StartTicks = 0;
            Window = 0;
        }

        [DataMember(Order = 0)] public string Name;
        [DataMember(Order = 1)] public string IP;
        [DataMember(Order = 2)] public string Port;
        [DataMember(Order = 3)] public string Executable;
        [DataMember(Order = 4)] public string Priority;
        [DataMember(Order = 5)] public bool Paused;
        [DataMember(Order = 6)] public int Restarts;
        [DataMember(Order = 7)] public bool HideConsole;
        [DataMember(Order = 8)] public string Cores;
        [DataMember(Order = 9)] public string Params;
        [DataMember(Order = 10)] public List<ScheduleEntry> Schedules;
        [DataMember(Order = 11)] public string RconPassword;
        [DataMember(Order = 12)] public string LastRestart;
        [DataMember(Order = 13)] public string Color;
        [DataMember(Order = 14)] public int Pid;
        [DataMember(Order = 15)] public long StartTicks;
        [DataMember(Order = 16)] public long Window;
    }

    [DataContract]
    public class ScheduleEntry
    {
        public ScheduleEntry() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            Enabled = true;
            Day = "Daily";
            Time = "05:00";
            Action = "Restart";
        }

        public static readonly string[] Days = { "Daily", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        public static readonly string[] Actions = { "Restart", "Start", "Stop" };

        [DataMember(Order = 0)] public bool Enabled;
        [DataMember(Order = 1)] public string Day;
        [DataMember(Order = 2)] public string Time;
        [DataMember(Order = 3)] public string Action;

        public bool Next(DateTime from, DateTime to, out DateTime at)
        {
            at = DateTime.MinValue;
            if (!Enabled) { return false; }
            TimeSpan time;
            if (!TimeSpan.TryParse(Time, out time)) { return false; }
            for (DateTime day = from.Date; day <= to.Date; day = day.AddDays(1))
            {
                if (Day != "Daily" && Day != day.DayOfWeek.ToString()) { continue; }
                at = day + time;
                if (at > from && at <= to) { return true; }
            }
            return false;
        }

        public ScheduleEntry copy()
        {
            ScheduleEntry c = new ScheduleEntry();
            c.Enabled = Enabled; c.Day = Day; c.Time = Time; c.Action = Action;
            return c;
        }

        public override string ToString()
        {
            return string.Format("{0}{1} {2}: {3}", Enabled ? "" : "(off) ", Day, Time, Action);
        }
    }

    [DataContract]
    public class ServerList
    {
        public ServerList() { defaults(); }
        [OnDeserializing] private void on_deserializing(StreamingContext context) { defaults(); }
        private void defaults()
        {
            Servers = new List<ServerEntry>();
        }

        [DataMember] public List<ServerEntry> Servers;
    }

    [DataContract]
    public class StatusFile
    {
        [DataMember(Order = 0)] public string Updated;
        [DataMember(Order = 1)] public string[] Columns;
        [DataMember(Order = 2)] public List<string[]> Rows;
    }

    public static class Json
    {
        public static T Load<T>(string path) where T : class
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(fs);
            }
        }

        public static void Save<T>(string path, T obj)
        {
            string text;
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(ms, obj);
                text = Encoding.UTF8.GetString(ms.ToArray());
            }
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, Indent(text), new UTF8Encoding(false));
            if (File.Exists(path)) { File.Replace(tmp, path, null); } else { File.Move(tmp, path); }
        }

        private static string Indent(string json)
        {
            StringBuilder sb = new StringBuilder();
            int depth = 0;
            bool quoted = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (quoted)
                {
                    sb.Append(c);
                    if (c == '\\') { sb.Append(json[++i]); }
                    else if (c == '"') { quoted = false; }
                    continue;
                }
                switch (c)
                {
                    case '"': quoted = true; sb.Append(c); break;
                    case '{':
                    case '[':
                        sb.Append(c);
                        if (json[i + 1] == '}' || json[i + 1] == ']') { sb.Append(json[++i]); break; }
                        depth++;
                        sb.Append("\r\n").Append(' ', depth * 2);
                        break;
                    case '}':
                    case ']': depth--; sb.Append("\r\n").Append(' ', depth * 2).Append(c); break;
                    case ',': sb.Append(c).Append("\r\n").Append(' ', depth * 2); break;
                    case ':': sb.Append(": "); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString() + "\r\n";
        }
    }
}
