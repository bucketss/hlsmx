using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

namespace hlsmx
{
    public partial class MainForm : Form
    {
        class PendingAction
        {
            public string Action;
            public DateTime Due;
            public bool Warn;
            public bool Started;
            public int LastWarning = int.MaxValue;
            public bool Waiting;
        }

        class ServerState
        {
            public List<ScheduleEntry> Schedules = new List<ScheduleEntry>();
            public List<DateTime> RestartTimes = new List<DateTime>();
            public string RconPassword = "";
            public string LastRestart = "";
            public string Color = "";
            public string IP = "";
            public string Port = "";
            public string Exe = "";
            public string Params = "";
            public string Cores = "";
            public string Priority = "Normal";
            public bool Hidden;
            public int Restarts;
            public int Pid;
            public long StartTicks;
            public long Window;
            public string Proc = "";
            public int ProcCount;
            public int ProcMax;
            public string Net = "";
            public int NetCount;
            public int NetMax;
            public string Map = "";
            public int Players = -1;
            public int MaxPlayers;
            public NetQuery.A2sInfo Info;
            public int Humans = -1;
            public DateTime EmptySince = DateTime.MinValue;
            public DateTime LaunchedAt = DateTime.MinValue;
            public bool Queued;
            public List<PendingAction> Queue = new List<PendingAction>();
            public PendingAction Pending { get { return Queue.Count > 0 ? Queue[0] : null; } }
            public void cancel_active() { Queue.RemoveAll(p => p.Started || p.Waiting); }
            public string HltvKey;
            public int HltvCount;
            public bool HltvKnown;
        }

        class NetJob
        {
            public ListViewItem Item;
            public string IP;
            public int Port;
            public string Password;
            public NetQuery.A2sInfo Result;
            public int Hltv;
            public string HltvKey;
            public bool HltvKnown;
        }

        private const string PROC_PAUSED = "Paused";
        private const string PROC_NORMAL = "Normal";
        private const string PROC_RESTARTING = "Restarting";
        private const string PROC_LOST = "Lost";
        private const string NET_PAUSED = "Paused";
        private const string NET_NORMAL = "Normal";
        private const string NET_TIMEOUT = "Timeout";
        private const string ACTION_RESTART = "Restart";
        private const string ACTION_START = "Start";
        private const string ACTION_STOP = "Stop";
        private const int COL_NAME = 0;
        private const int COL_IP = 1;
        private const int COL_PORT = 2;
        private const int COL_EXE = 3;
        private const int COL_PRIORITY = 4;
        private const int COL_PROCESS = 5;
        private const int COL_NETWORK = 6;
        private const int COL_MAP = 7;
        private const int COL_PLAYERS = 8;
        private const int COL_RESTARTS = 9;
        private const int COL_PID = 10;
        private const int COL_HWND = 11;
        private const int COL_CONSOLE = 12;
        private const int COL_CPU = 13;
        private const int COL_PARAMS = 14;
        private const int COL_UPTIME = 15;
        private const int COL_LAST_RESTART = 16;
        private const int max_log_chars = 262144;
        private static readonly int[] default_order = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 15, 16, 10, 11, 12, 13, 14 };
        private static readonly int[] default_hidden = { 10, 11, 12, 13, 14, 16 };

        private bool start_hidden;
        private bool service_mode;
        private bool exiting = false;
        private bool net_busy = false;
        private readonly List<KeyValuePair<ListViewItem, string>> restart_queue = new List<KeyValuePair<ListViewItem, string>>();
        private readonly int ui_thread;
        private DateTime schedule_horizon = DateTime.Now;
        private const int warn_lookahead = 660;
        private const int empty_minutes = 5;
        private const int crash_loop_minutes = 5;
        private static readonly int auto_restart_slots = Math.Max(1, Math.Min(4, Environment.ProcessorCount / 4));
        private const int answer_timeout_seconds = 30;
        private static readonly int[] warning_seconds = { 600, 300, 120, 60, 30, 10, 5 };
        private readonly System.Windows.Forms.Timer pending_timer = new System.Windows.Forms.Timer();
        private int[] column_width;
        private bool[] column_visible;

        public MainForm(bool start_hidden, bool service_mode)
        {
            ui_thread = Thread.CurrentThread.ManagedThreadId;
            this.start_hidden = start_hidden || service_mode;
            this.service_mode = service_mode;
            InitializeComponent();
            menuAddServer.ShortcutKeyDisplayString = "Ctrl+N";
            menuAddServer.Click += menuNewServer_Click;
            menuSettings.DropDownItems.Insert(0, menuAddServer);
            setup_row_drag();
            Core.Instance.log_sink = log;
            string error = Core.Instance.ReadConfig();
            if (Core.Instance.opt_start_minimized) { this.start_hidden = true; }
            apply_language();
            if (service_mode) { trayIcon.Visible = false; }
            ApplyIcon();
            Theme.Apply(this);
            apply_layout();
            serverList.ClientSizeChanged += (s, e) => fill_last_column();
            serverList.ColumnReordered += (s, e) => BeginInvoke((MethodInvoker)fill_last_column);
            log(Lang.T(service_mode ? "log.started_service" : "log.started"));
            Core.Instance.notify(Core.NOTIFY_START, Lang.F(service_mode ? "notify.started_service" : "notify.started", Environment.MachineName));
            if (error != null) { log(error); }
            load_servers();
            checkTimer.Interval = Core.Instance.opt_check_interval * 1000;
            checkTimer.Enabled = true;
            pending_timer.Interval = 1000;
            pending_timer.Tick += (s, e) => { foreach (ListViewItem item in serverList.Items.Cast<ListViewItem>().ToList()) { run_pending(item); } };
            pending_timer.Enabled = true;
        }
        protected override void SetVisibleCore(bool value)
        {
            if (start_hidden)
            {
                if (!service_mode) { start_hidden = false; }
                if (!IsHandleCreated) { CreateHandle(); }
                value = false;
            }
            base.SetVisibleCore(value);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.WM_SHOWME && !service_mode) { ShowFromTray(); }
            base.WndProc(ref m);
        }
        public void ApplyIcon()
        {
            this.Icon = Core.Instance.app_icon;
            Icon old = trayIcon.Icon;
            trayIcon.Icon = new Icon(this.Icon, SystemInformation.SmallIconSize);
            if (old != null) { old.Dispose(); }
        }
        public void ExitApp()
        {
            exiting = true;
            this.Close();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!service_mode) { save_layout(); }
            if (!exiting && e.CloseReason == CloseReason.UserClosing && Core.Instance.opt_tray)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            trayIcon.Visible = false;
            Core.Instance.log_sink = null;
        }
        private void ShowFromTray()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized) { this.WindowState = FormWindowState.Normal; }
            this.Activate();
        }
        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) { return; }
            if (this.Visible && this.WindowState != FormWindowState.Minimized) { this.Hide(); }
            else { ShowFromTray(); }
        }
        private void menuTrayShow_Click(object sender, EventArgs e)
        {
            ShowFromTray();
        }
        public void log(string text)
        {
            string line = string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), text);
            Core.Instance.write_log(line);
            MethodInvoker append = delegate
            {
                if (logBox.IsDisposed) { return; }
                logBox.AppendText((logBox.TextLength > 0 ? "\r\n" : "") + line);
                if (logBox.TextLength <= max_log_chars) { return; }
                string kept = logBox.Text;
                int cut = kept.IndexOf('\n', kept.Length - max_log_chars / 2);
                logBox.Text = cut < 0 ? "" : kept.Substring(cut + 1);
                logBox.SelectionStart = logBox.TextLength;
                logBox.ScrollToCaret();
            };
            if (Thread.CurrentThread.ManagedThreadId == ui_thread) { append(); return; }
            try { this.BeginInvoke(append); }
            catch (InvalidOperationException) { }
        }
        private void log_event(string kind, string text)
        {
            log(text);
            Core.Instance.notify(kind, text);
        }

        private void apply_layout()
        {
            Settings layout = Core.Instance.layout;
            int count = serverList.Columns.Count;
            column_width = serverList.Columns.Cast<ColumnHeader>().Select(c => c.Width).ToArray();
            column_visible = Enumerable.Range(0, count).Select(i => !default_hidden.Contains(i)).ToArray();
            List<int> order = default_order.ToList();
            if (layout.Columns != null && layout.Columns.Count > 0)
            {
                foreach (ColumnSetting c in layout.Columns.Where(c => c.Index >= 0 && c.Index < count))
                {
                    if (c.Width > 0) { column_width[c.Index] = c.Width; }
                    column_visible[c.Index] = c.Visible || c.Index == 0;
                }
                List<int> saved = layout.Columns.Where(c => c.Index >= 0 && c.Index < count).OrderBy(c => c.DisplayIndex).Select(c => c.Index).Distinct().ToList();
                order = saved.Concat(default_order.Where(i => !saved.Contains(i))).ToList();
            }
            for (int i = 0; i < count; i++) { serverList.Columns[i].Width = column_visible[i] ? column_width[i] : 0; }
            for (int i = 0; i < order.Count; i++) { serverList.Columns[order[i]].DisplayIndex = i; }

            if (layout.WindowBounds != null && layout.WindowBounds.Length == 4)
            {
                Rectangle bounds = new Rectangle(layout.WindowBounds[0], layout.WindowBounds[1], layout.WindowBounds[2], layout.WindowBounds[3]);
                if (bounds.Width >= 200 && bounds.Height >= 100 && Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds)))
                {
                    this.StartPosition = FormStartPosition.Manual;
                    this.Bounds = bounds;
                }
            }
            this.Load += (s, e) => fit_window_to_columns();
            if (layout.WindowMaximized) { this.WindowState = FormWindowState.Maximized; }
        }
        private void save_layout()
        {
            Settings layout = Core.Instance.layout;
            Rectangle bounds = this.WindowState == FormWindowState.Normal ? this.Bounds : this.RestoreBounds;
            layout.WindowBounds = new int[] { bounds.X, bounds.Y, bounds.Width, bounds.Height };
            layout.WindowMaximized = this.WindowState == FormWindowState.Maximized;
            layout.Columns = new List<ColumnSetting>();
            for (int i = 0; i < serverList.Columns.Count; i++)
            {
                ColumnSetting c = new ColumnSetting();
                c.Index = i;
                c.Width = column_width[i];
                c.DisplayIndex = serverList.Columns[i].DisplayIndex;
                c.Visible = column_visible[i];
                layout.Columns.Add(c);
            }
            try { Core.Instance.SaveConfig(); }
            catch (Exception e) { log(Lang.F("log.save_settings_failed", e.Message)); }
        }

        private static string ip_display(string ip)
        {
            return !Core.Instance.opt_list_local_ips && NetQuery.Instance.is_local(ip) ? Lang.T("ip.local") : ip;
        }
        private static string status_text(string status, int count, int max)
        {
            switch (status)
            {
                case PROC_PAUSED: return Lang.T("status.paused");
                case PROC_NORMAL: return Lang.T("status.normal");
                case PROC_RESTARTING: return Lang.T("status.restarting");
                case PROC_LOST: return Lang.F("status.lost", count, max);
                case NET_TIMEOUT: return Lang.F("status.timeout", count, max);
            }
            return "-";
        }
        private static string proc_text(ServerState st)
        {
            if (st.Queued) { return Lang.T("status.queued"); }
            if (st.Pending != null && st.Proc == PROC_NORMAL && (st.Pending.Started || st.Pending.Waiting)) { return Lang.T(st.Pending.Action == ACTION_STOP ? "status.pending_stop" : "status.pending_restart"); }
            return status_text(st.Proc, st.ProcCount, st.ProcMax);
        }
        private static void set_proc(ServerState st, string status) { set_proc(st, status, 0, 0); }
        private static void set_proc(ServerState st, string status, int count, int max)
        {
            st.Proc = status;
            st.ProcCount = count;
            st.ProcMax = max;
        }
        private static void set_net(ServerState st, string status) { set_net(st, status, 0, 0); }
        private static void set_net(ServerState st, string status, int count, int max)
        {
            st.Net = status;
            st.NetCount = count;
            st.NetMax = max;
        }
        private static void clear_query(ServerState st)
        {
            st.Map = "";
            st.Players = -1;
            st.MaxPlayers = 0;
            st.Info = null;
            st.Humans = -1;
            st.EmptySince = DateTime.MinValue;
        }
        private static void clear_process(ServerState st)
        {
            st.Pid = 0;
            st.StartTicks = 0;
            st.Window = 0;
        }
        private static bool is_paused(ListViewItem item)
        {
            return state_of(item).Proc == PROC_PAUSED;
        }
        private static void show(ListViewItem item)
        {
            ServerState st = state_of(item);
            set_text(item, COL_IP, ip_display(st.IP));
            set_text(item, COL_PORT, st.Port);
            set_text(item, COL_EXE, st.Exe);
            set_text(item, COL_PRIORITY, Lang.priority(st.Priority));
            set_text(item, COL_PROCESS, proc_text(st));
            set_text(item, COL_NETWORK, status_text(st.Net, st.NetCount, st.NetMax));
            set_text(item, COL_MAP, st.Map.Length > 0 ? st.Map : "-");
            set_text(item, COL_PLAYERS, st.Players >= 0 ? st.Players + "/" + st.MaxPlayers : "-");
            set_text(item, COL_RESTARTS, st.Restarts.ToString());
            set_text(item, COL_PID, st.Pid.ToString());
            set_text(item, COL_HWND, st.Window.ToString());
            set_text(item, COL_CONSOLE, Lang.T(st.Hidden ? "console.hidden" : "console.shown"));
            set_text(item, COL_CPU, st.Cores);
            set_text(item, COL_PARAMS, st.Params);
            set_text(item, COL_UPTIME, st.Pid > 0 ? format_uptime(st.StartTicks) : "-");
            set_text(item, COL_LAST_RESTART, format_last_restart(st.LastRestart));
        }
        private static void set_text(ListViewItem item, int column, string text)
        {
            if (!item.SubItems[column].Text.Equals(text)) { item.SubItems[column].Text = text; }
        }

        private static readonly string[] column_keys = { "col.name", "col.ip", "col.port", "col.exe", "col.priority", "col.process", "col.network", "col.map", "col.players", "col.restarts", "col.pid", "col.hwnd", "col.console", "col.cpu", "col.params", "col.uptime", "col.last_restart" };
        private bool menu_resumes;

        private void apply_language()
        {
            this.Text = Lang.T("app.title");
            menuSettings.Text = Lang.T("menu.settings");
            menuAddServer.Text = Lang.T("menu.new_server");
            menuOptions.Text = Lang.T("menu.options");
            menuOpenLogs.Text = Lang.T("menu.open_logs");
            menuExit.Text = Lang.T("menu.exit");
            menuHelp.Text = Lang.T("menu.help");
            menuAbout.Text = Lang.T("menu.about");
            tabServers.Text = Lang.T("tab.servers");
            tabLog.Text = Lang.T("tab.log");
            for (int i = 0; i < serverList.Columns.Count && i < column_keys.Length; i++) { serverList.Columns[i].Text = Lang.T(column_keys[i]); }
            set_pause_menu(menu_resumes);
            menuResetRestartCount.Text = Lang.T("ctx.zero");
            menuEditServer.Text = Lang.T("ctx.edit");
            menuSchedules.Text = Lang.T("ctx.schedules");
            menuNewServer.Text = Lang.T("ctx.new");
            menuDuplicateServer.Text = Lang.T("ctx.duplicate");
            menuDeleteServer.Text = Lang.T("ctx.delete");
            menuMoveUp.Text = Lang.T("ctx.move_up");
            menuMoveDown.Text = Lang.T("ctx.move_down");
            menuShowServer.Text = Lang.T("ctx.show_console");
            menuHideServer.Text = Lang.T("ctx.hide_console");
            menuRestartServer.Text = Lang.T("ctx.restart");
            menuCloseServer.Text = Lang.T("ctx.close");
            menuTrayShow.Text = Lang.T("tray.show");
            menuTrayExit.Text = Lang.T("menu.exit");
            build_color_menu();
            serverList.BeginUpdate();
            foreach (ListViewItem item in serverList.Items) { show(item); }
            serverList.EndUpdate();
        }

        private void set_pause_menu(bool resume)
        {
            menu_resumes = resume;
            menuPauseMonitoring.Text = Lang.T(resume ? "ctx.resume" : "ctx.pause");
        }

        private static ServerState state_of(ListViewItem item)
        {
            if (item.Tag == null) { item.Tag = new ServerState(); }
            return (ServerState)item.Tag;
        }
        private ServerEntry entry_of(ListViewItem item)
        {
            ServerState st = state_of(item);
            ServerEntry s = new ServerEntry();
            s.Name = item.Text;
            s.IP = st.IP;
            s.Port = st.Port;
            s.Executable = st.Exe;
            s.Priority = st.Priority;
            s.Paused = is_paused(item);
            s.Restarts = st.Restarts;
            s.HideConsole = st.Hidden;
            s.Cores = st.Cores;
            s.Params = st.Params;
            s.Schedules = st.Schedules;
            s.RconPassword = st.RconPassword;
            s.LastRestart = st.LastRestart;
            s.Color = st.Color;
            s.Pid = st.Pid;
            s.StartTicks = st.StartTicks;
            s.Window = st.Window;
            return s;
        }
        private ListViewItem add_item(ServerEntry s, int index)
        {
            ListViewItem item = new ListViewItem(s.Name);
            for (int i = 1; i < serverList.Columns.Count; i++) { item.SubItems.Add(""); }
            ServerState st = state_of(item);
            st.IP = s.IP ?? "";
            st.Port = s.Port ?? "";
            st.Exe = s.Executable ?? "";
            st.Params = s.Params ?? "";
            st.Cores = s.Cores ?? "";
            st.Priority = string.IsNullOrEmpty(s.Priority) ? "Normal" : s.Priority;
            st.Hidden = s.HideConsole;
            st.Restarts = s.Restarts;
            st.Schedules = s.Schedules ?? new List<ScheduleEntry>();
            st.RconPassword = s.RconPassword ?? "";
            st.LastRestart = s.LastRestart ?? "";
            st.Color = s.Color ?? "";
            st.Pid = s.Pid;
            st.StartTicks = s.StartTicks;
            st.Window = s.Window;
            set_proc(st, s.Paused ? PROC_PAUSED : "");
            set_net(st, s.Paused ? NET_PAUSED : "");
            apply_row_color(item);
            show(item);
            if (index < 0 || index >= serverList.Items.Count) { serverList.Items.Add(item); } else { serverList.Items.Insert(index, item); }
            return item;
        }
        public void save_servers()
        {
            List<ServerEntry> servers = serverList.Items.Cast<ListViewItem>().Select(entry_of).ToList();
            try { Core.Instance.SaveConfig_Servers(servers); }
            catch (Exception e) { log(Lang.F("log.save_servers_failed", e.Message)); }
        }
        public void load_servers()
        {
            string error;
            foreach (ServerEntry s in Core.Instance.ReadConfig_Servers(out error)) { add_item(s, -1); }
            if (error != null) { log(error); }
        }

        private static string format_last_restart(string value)
        {
            DateTime time;
            return DateTime.TryParse(value, out time) ? time.ToString("yyyy-MM-dd HH:mm") : "-";
        }
        private static string format_uptime(long start_filetime)
        {
            if (start_filetime <= 0) { return "-"; }
            TimeSpan up = DateTime.Now - DateTime.FromFileTime(start_filetime);
            if (up.TotalSeconds < 0) { return "-"; }
            return up.Days > 0 ? string.Format("{0}d {1:00}:{2:00}", up.Days, up.Hours, up.Minutes) : string.Format("{0:00}:{1:00}:{2:00}", up.Hours, up.Minutes, up.Seconds);
        }

        private void menuAbout_Click(object sender, EventArgs e)
        {
            using (AboutForm form = new AboutForm()) { form.ShowDialog(this); }
        }
        private void menuExit_Click(object sender, EventArgs e)
        {
            ExitApp();
        }
        private void menuOpenLogs_Click(object sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Core.logs_dir);
                Process.Start("explorer.exe", "\"" + Core.logs_dir + "\"");
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private void menuOptions_Click(object sender, EventArgs e)
        {
            using (OptionsForm form = new OptionsForm())
            {
                form.StartPosition = FormStartPosition.CenterParent;
                form.import_configs = () => import_configs(form);
                form.applied = apply_options;
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                if (form.service_installed)
                {
                    log(Lang.T("log.service_installed"));
                    ExitApp();
                    return;
                }
            }
            apply_options();
        }
        private void apply_options()
        {
            checkTimer.Interval = Core.Instance.opt_check_interval * 1000;
            Lang.load(Lang.resolve(Core.Instance.opt_language));
            apply_language();
            ApplyIcon();
            Theme.Apply(this);
        }
        private void import_configs(IWin32Window owner)
        {
            string path;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = Lang.T("import.filter");
                if (dialog.ShowDialog(owner) != DialogResult.OK) { return; }
                path = dialog.FileName;
            }
            string folder = Path.GetDirectoryName(path);
            try
            {
                if (path.EndsWith(".l24", StringComparison.OrdinalIgnoreCase))
                {
                    List<ServerEntry> servers = HlsmImport.read_servers(path);
                    int added = add_imported(servers);
                    log(Lang.F("log.imported", added, servers.Count - added));
                    string ini = Path.Combine(folder, "hlsm.ini");
                    if (File.Exists(ini) && ask_import(owner, "import.ini_question")) { import_hlsm_ini(ini); }
                    return;
                }
                if (path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
                {
                    import_hlsm_ini(path);
                    return;
                }
                string[] lines = File.ReadAllLines(path);
                if (CfImport.is_servers(lines))
                {
                    List<ServerEntry> servers = CfImport.read_servers(path);
                    int added = add_imported(servers);
                    log(Lang.F("log.imported_cf", added, servers.Count - added));
                    string options = Path.Combine(folder, CfImport.options_file);
                    if (File.Exists(options) && ask_import(owner, "import.cf_options_question")) { import_cf_options(options); }
                    return;
                }
                if (CfImport.is_options(lines))
                {
                    import_cf_options(path);
                    return;
                }
                MessageBox.Show(owner, Lang.F("import.unknown", Path.GetFileName(path)), Lang.T("dlg.import"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, Lang.F("import.read_failed", Path.GetFileName(path), ex.Message), Lang.T("dlg.import"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ask_import(IWin32Window owner, string key)
        {
            return MessageBox.Show(owner, Lang.T(key), Lang.T("dlg.import"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private static string server_key(string exe, string port)
        {
            return (exe ?? "").Trim().ToLowerInvariant() + "|" + (port ?? "").Trim();
        }

        private int add_imported(List<ServerEntry> servers)
        {
            HashSet<string> existing = new HashSet<string>(serverList.Items.Cast<ListViewItem>().Select(i => server_key(state_of(i).Exe, state_of(i).Port)));
            int added = 0;
            foreach (ServerEntry s in servers)
            {
                if (!existing.Add(server_key(s.Executable, s.Port))) { continue; }
                s.Paused = true;
                add_item(s, -1);
                added++;
            }
            save_servers();
            return added;
        }

        private void import_hlsm_ini(string path)
        {
            Dictionary<string, int> values = HlsmImport.read_settings(path);
            int sec, prc, nwc, max;
            bool any = values.TryGetValue("CheckInterval", out sec) | values.TryGetValue("ProcessCheckTimes", out prc) | values.TryGetValue("NetworkCheckTimes", out nwc) | values.TryGetValue("MaxStartCount", out max);
            if (!any) { throw new InvalidDataException(Lang.F("import.unknown", Path.GetFileName(path))); }
            if (values.ContainsKey("CheckInterval")) { Core.Instance.opt_check_interval = sec; }
            if (values.ContainsKey("ProcessCheckTimes")) { Core.Instance.opt_process_retries = prc; }
            if (values.ContainsKey("NetworkCheckTimes")) { Core.Instance.opt_network_retries = nwc; }
            if (values.ContainsKey("MaxStartCount")) { Core.Instance.opt_max_restarts = max; }
            save_check_settings();
            log(Lang.T("log.imported_ini"));
        }

        private void import_cf_options(string path)
        {
            int[] values = CfImport.read_settings(path);
            Core.Instance.opt_check_interval = values[0];
            Core.Instance.opt_process_retries = values[1];
            Core.Instance.opt_network_retries = values[2];
            Core.Instance.opt_max_restarts = values[3];
            if (values.Length > 4) { Core.Instance.opt_tray = values[4] != 0; }
            save_check_settings();
            log(Lang.T("log.imported_cf_options"));
        }

        private void save_check_settings()
        {
            Core.Instance.normalize();
            Core.Instance.SaveConfig();
            checkTimer.Interval = Core.Instance.opt_check_interval * 1000;
        }
        private void menuResetRestartCount_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in serverList.SelectedItems)
            {
                ServerState st = state_of(lvi);
                st.Restarts = 0;
                st.RestartTimes.Clear();
                show(lvi);
            }
            save_servers();
        }
        private void menuPauseMonitoring_Click(object sender, EventArgs e)
        {
            set_selected_state(menu_resumes);
        }
        private void set_console_hidden(bool hidden)
        {
            foreach (ListViewItem lvi in serverList.SelectedItems)
            {
                ServerState st = state_of(lvi);
                st.Hidden = hidden;
                Core.show_window(st.Window, !hidden);
                show(lvi);
            }
            save_servers();
        }
        private void menuHideServer_Click(object sender, EventArgs e)
        {
            set_console_hidden(true);
        }
        private void menuShowServer_Click(object sender, EventArgs e)
        {
            set_console_hidden(false);
        }

        private void serverList_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            if (!column_visible[e.ColumnIndex]) { e.NewWidth = 0; e.Cancel = true; }
        }
        private void serverList_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (filling || (e.ColumnIndex == stretched_index && serverList.Columns[e.ColumnIndex].Width == stretched_width)) { return; }
            if (column_visible != null && column_visible[e.ColumnIndex] && serverList.Columns[e.ColumnIndex].Width > 0) { column_width[e.ColumnIndex] = serverList.Columns[e.ColumnIndex].Width; }
            fill_last_column();
        }
        private bool filling;
        private int stretched_index = -1;
        private int stretched_width;
        private void fill_last_column()
        {
            if (column_visible == null || filling) { return; }
            List<ColumnHeader> visible = serverList.Columns.Cast<ColumnHeader>().Where(c => column_visible[c.Index]).OrderBy(c => c.DisplayIndex).ToList();
            if (visible.Count == 0) { return; }
            filling = true;
            try
            {
                ColumnHeader last = visible[visible.Count - 1];
                foreach (ColumnHeader c in visible) { if (c != last && c.Width != column_width[c.Index]) { c.Width = column_width[c.Index]; } }
                int width = Math.Max(column_width[last.Index], serverList.ClientSize.Width - visible.Where(c => c != last).Sum(c => c.Width));
                stretched_index = last.Index;
                stretched_width = width;
                if (last.Width != width) { last.Width = width; }
            }
            finally { filling = false; }
        }
        private void headerMenu_Opening(object sender, CancelEventArgs e)
        {
            if (!Theme.header_bounds(serverList).Contains(Cursor.Position)) { e.Cancel = true; return; }
            headerMenu.Items.Clear();
            foreach (ColumnHeader column in serverList.Columns.Cast<ColumnHeader>().OrderBy(c => c.DisplayIndex))
            {
                int index = column.Index;
                ToolStripMenuItem item = new ToolStripMenuItem(column.Text);
                item.Checked = column_visible[index];
                item.Enabled = index != COL_NAME;
                item.Click += (s, a) => set_column_visible(index, !column_visible[index]);
                headerMenu.Items.Add(item);
            }
            headerMenu.Items.Add(new ToolStripSeparator());
            headerMenu.Items.Add(Lang.T("hdr.reset"), null, (s, a) => reset_columns());
        }
        private void set_column_visible(int index, bool visible)
        {
            column_visible[index] = visible;
            if (visible && column_width[index] < 20) { column_width[index] = 90; }
            serverList.Columns[index].Width = visible ? column_width[index] : 0;
            fit_window_to_columns();
            fill_last_column();
        }
        private void fit_window_to_columns()
        {
            if (this.WindowState != FormWindowState.Normal) { return; }
            int needed = Enumerable.Range(0, column_width.Length).Where(i => column_visible[i]).Sum(i => column_width[i]);
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int width = Math.Max(this.MinimumSize.Width, Math.Max(300, Math.Min(area.Width, this.Width + needed - serverList.ClientSize.Width)));
            int left = Math.Max(area.Left, Math.Min(this.Left, area.Right - width));
            this.SetBounds(left, this.Top, width, this.Height);
        }
        private void reset_columns()
        {
            Core.Instance.layout.Columns = null;
            int[] widths = { 90, 100, 50, 220, 70, 90, 90, 110, 60, 60, 60, 90, 60, 90, 200, 70, 110 };
            for (int i = 0; i < serverList.Columns.Count; i++)
            {
                column_width[i] = widths[i];
                column_visible[i] = !default_hidden.Contains(i);
                serverList.Columns[i].Width = column_visible[i] ? widths[i] : 0;
            }
            for (int i = 0; i < default_order.Length; i++) { serverList.Columns[default_order[i]].DisplayIndex = i; }
            fit_window_to_columns();
            fill_last_column();
        }

        private void set_item_state(ListViewItem lvi, bool start)
        {
            ServerState st = state_of(lvi);
            unqueue(lvi);
            set_proc(st, start ? "" : PROC_PAUSED);
            set_net(st, start ? "" : NET_PAUSED);
            clear_query(st);
            st.RestartTimes.Clear();
        }
        private void set_selected_state(bool start)
        {
            foreach (ListViewItem lvi in serverList.SelectedItems.Cast<ListViewItem>().ToList())
            {
                state_of(lvi).cancel_active();
                if (start)
                {
                    if (!is_paused(lvi) || !confirm_port_free(lvi)) { continue; }
                    set_item_state(lvi, true);
                    if (state_of(lvi).Pid == 0) { restart_item(lvi, 0); }
                }
                else { set_item_state(lvi, false); }
                show(lvi);
            }
            save_servers();
        }

        private static bool binds_all(string ip)
        {
            System.Net.IPAddress address;
            return !System.Net.IPAddress.TryParse(ip ?? "", out address) || System.Net.IPAddress.IsLoopback(address) || address.Equals(System.Net.IPAddress.Any);
        }

        private bool port_conflict(ListViewItem item, out ListViewItem other, out int pid)
        {
            other = null;
            pid = 0;
            ServerState st = state_of(item);
            string port = st.Port.Trim();
            int port_number;
            if (!Int32.TryParse(port, out port_number)) { return false; }
            foreach (ListViewItem candidate in serverList.Items)
            {
                ServerState cs = state_of(candidate);
                if (candidate == item || cs.Port.Trim() != port) { continue; }
                if (cs.IP != st.IP && !binds_all(st.IP) && !binds_all(cs.IP)) { continue; }
                if (cs.Proc == PROC_RESTARTING || cs.Pid > 0) { other = candidate; return true; }
            }
            int owner = NetQuery.Instance.udp_port_owner(st.IP, port_number);
            if (owner <= 0 || owner == st.Pid) { return false; }
            pid = owner;
            return true;
        }

        private bool confirm_port_free(ListViewItem item)
        {
            ListViewItem other;
            int pid;
            if (!port_conflict(item, out other, out pid)) { return true; }
            string text = other != null ? Lang.F("msg.port_conflict", other.Text, item.Text) : Lang.F("msg.port_conflict_pid", pid, item.Text);
            MessageBox.Show(this, text, Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        private bool port_free_or_pause(ListViewItem item)
        {
            ListViewItem other;
            int pid;
            if (!port_conflict(item, out other, out pid)) { return true; }
            ServerState st = state_of(item);
            if (other != null) { log(Lang.F("log.port_conflict", item.Text, st.Port, other.Text)); }
            else { log(Lang.F("log.port_conflict_pid", item.Text, st.Port, pid)); }
            set_item_state(item, false);
            show(item);
            save_servers();
            return false;
        }

        private void serverList_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) { return; }
            List<int> rows = serverList.SelectedIndices.Cast<int>().ToList();
            bool any = rows.Count > 0;
            bool single = rows.Count == 1;
            ServerState first = any ? state_of(serverList.Items[rows[0]]) : null;
            foreach (ToolStripItem entry in serverMenu.Items) { entry.Enabled = any; }
            menuNewServer.Enabled = true;
            set_pause_menu(any && first.Proc == PROC_PAUSED);
            if (any)
            {
                menuResetRestartCount.Enabled = first.Proc != PROC_PAUSED;
                menuEditServer.Enabled = single;
                menuSchedules.Enabled = single;
                menuDuplicateServer.Enabled = single;
                menuShowServer.Visible = !single || first.Hidden;
                menuHideServer.Visible = !single || !first.Hidden;
                menuCloseServer.Enabled = !single || first.Pid > 0;
                menuMoveUp.Enabled = rows.Min() > 0;
                menuMoveDown.Enabled = rows.Max() < serverList.Items.Count - 1;
            }
            serverMenu.Show(Cursor.Position);
        }

        private void serverList_DoubleClick(object sender, EventArgs e)
        {
            if (serverList.SelectedItems.Count == 1) { menuEditServer_Click(sender, e); }
        }

        private void serverList_KeyDown(object sender, KeyEventArgs e)
        {
            int selected = serverList.SelectedItems.Count;
            if (e.KeyCode == Keys.Delete && selected > 0) { menuDeleteServer_Click(sender, e); }
            else if ((e.KeyCode == Keys.Enter || e.KeyCode == Keys.F2) && selected == 1) { menuEditServer_Click(sender, e); }
            else if (e.Control && e.KeyCode == Keys.N) { menuNewServer_Click(sender, e); }
            else if (e.Control && e.KeyCode == Keys.D && selected == 1) { menuDuplicateServer_Click(sender, e); }
            else if (e.Control && e.KeyCode == Keys.Up && selected > 0) { move_selected(-1); }
            else if (e.Control && e.KeyCode == Keys.Down && selected > 0) { move_selected(1); }
            else if (e.Control && e.KeyCode == Keys.A) { foreach (ListViewItem lvi in serverList.Items) { lvi.Selected = true; } }
            else if (e.KeyCode == Keys.Space && selected > 0) { set_selected_state(is_paused(serverList.SelectedItems[0])); }
            else { return; }
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private int sort_column = -1;
        private bool sort_descending;

        private void serverList_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            sort_descending = e.Column == sort_column && !sort_descending;
            sort_column = e.Column;
            int column = e.Column;
            List<ListViewItem> sorted = serverList.Items.Cast<ListViewItem>().ToList();
            sorted.Sort((a, b) => { int c = compare_cells(a.SubItems[column].Text, b.SubItems[column].Text, column); return c != 0 ? c : a.Index.CompareTo(b.Index); });
            if (sort_descending) { sorted.Reverse(); }
            serverList.BeginUpdate();
            serverList.Items.Clear();
            serverList.Items.AddRange(sorted.ToArray());
            serverList.EndUpdate();
            save_servers();
        }

        private static int compare_cells(string a, string b, int column)
        {
            bool a_empty = a == "" || a == "-";
            bool b_empty = b == "" || b == "-";
            if (a_empty || b_empty) { return a_empty == b_empty ? 0 : (a_empty ? 1 : -1); }
            if (column == COL_UPTIME) { return uptime_seconds(a).CompareTo(uptime_seconds(b)); }
            int players_a, players_b;
            if (column == COL_PLAYERS && Int32.TryParse(a.Split('/')[0], out players_a) && Int32.TryParse(b.Split('/')[0], out players_b)) { return players_a.CompareTo(players_b); }
            System.Net.IPAddress ip_a, ip_b;
            if (System.Net.IPAddress.TryParse(a, out ip_a) && System.Net.IPAddress.TryParse(b, out ip_b) && a.Contains('.') && b.Contains('.'))
            {
                byte[] x = ip_a.GetAddressBytes(), y = ip_b.GetAddressBytes();
                for (int i = 0; i < Math.Min(x.Length, y.Length); i++) { if (x[i] != y[i]) { return x[i].CompareTo(y[i]); } }
                return x.Length.CompareTo(y.Length);
            }
            double num_a, num_b;
            if (double.TryParse(a, out num_a) && double.TryParse(b, out num_b)) { return num_a.CompareTo(num_b); }
            return StringComparer.OrdinalIgnoreCase.Compare(a, b);
        }

        private static long uptime_seconds(string text)
        {
            long days = 0;
            int d = text.IndexOf('d');
            if (d > 0) { long.TryParse(text.Substring(0, d), out days); text = text.Substring(d + 1).Trim(); }
            long total = 0;
            foreach (string part in text.Split(':')) { long v; long.TryParse(part, out v); total = total * 60 + v; }
            if (d > 0) { total *= 60; }
            return days * 86400 + total;
        }

        private static readonly string[][] row_colors = {
            new[] { "color.green", "#C8F0C8" },
            new[] { "color.red", "#F8C8C8" },
            new[] { "color.blue", "#C8DCF8" },
            new[] { "color.yellow", "#F8F0B4" },
            new[] { "color.purple", "#E0C8F0" }
        };

        private ToolStripMenuItem color_menu;

        private void build_color_menu()
        {
            if (color_menu != null)
            {
                serverMenu.Items.Remove(color_menu);
                color_menu.Dispose();
            }
            ToolStripMenuItem menu = new ToolStripMenuItem(Lang.T("ctx.color"));
            menu.DropDownItems.Add(new ToolStripMenuItem(Lang.T("color.none"), null, (s, e) => set_row_color("")));
            foreach (string[] entry in row_colors)
            {
                string value = entry[1];
                Color color;
                ColorWheelForm.try_parse(value, out color);
                menu.DropDownItems.Add(new ToolStripMenuItem(Lang.T(entry[0]), swatch(color), (s, e) => set_row_color(value)));
            }
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(new ToolStripMenuItem(Lang.T("color.other"), null, (s, e) => pick_row_color()));
            serverMenu.Items.Insert(serverMenu.Items.IndexOf(menuMoveDown) + 1, menu);
            color_menu = menu;
        }

        private static Bitmap swatch(Color color)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                using (SolidBrush b = new SolidBrush(color)) { g.FillRectangle(b, 1, 1, 14, 14); }
                using (Pen p = new Pen(Color.Gray)) { g.DrawRectangle(p, 1, 1, 13, 13); }
            }
            return bmp;
        }

        private void pick_row_color()
        {
            if (serverList.SelectedItems.Count == 0) { return; }
            Color initial;
            ColorWheelForm.try_parse(state_of(serverList.SelectedItems[0]).Color, out initial);
            using (ColorWheelForm form = new ColorWheelForm(initial))
            {
                if (form.ShowDialog(this) == DialogResult.OK) { set_row_color(ColorWheelForm.to_hex(form.Selected)); }
            }
        }

        private void set_row_color(string value)
        {
            if (serverList.SelectedItems.Count == 0) { return; }
            foreach (ListViewItem item in serverList.SelectedItems)
            {
                state_of(item).Color = value;
                apply_row_color(item);
            }
            save_servers();
        }

        private void apply_row_color(ListViewItem item)
        {
            Color color;
            if (ColorWheelForm.try_parse(state_of(item).Color, out color))
            {
                item.BackColor = color;
                item.ForeColor = Color.Black;
            }
            else
            {
                item.BackColor = Color.Empty;
                item.ForeColor = Color.Empty;
            }
        }

        private List<ListViewItem> drag_items;
        private bool drag_moved;

        private void setup_row_drag()
        {
            serverList.AllowDrop = true;
            serverList.ItemDrag += serverList_ItemDrag;
            serverList.DragEnter += serverList_DragOver;
            serverList.DragOver += serverList_DragOver;
        }

        private void serverList_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button != MouseButtons.Left || serverList.SelectedItems.Count == 0) { return; }
            drag_items = serverList.SelectedItems.Cast<ListViewItem>().OrderBy(i => i.Index).ToList();
            drag_moved = false;
            try { serverList.DoDragDrop(drag_items, DragDropEffects.Move); }
            finally
            {
                drag_items = null;
                if (drag_moved)
                {
                    sort_column = -1;
                    save_servers();
                }
            }
        }

        private void serverList_DragOver(object sender, DragEventArgs e)
        {
            if (drag_items == null || serverList.Items.Count == 0) { e.Effect = DragDropEffects.None; return; }
            e.Effect = DragDropEffects.Move;
            Point p = serverList.PointToClient(new Point(e.X, e.Y));
            scroll_for_drag(p);
            ListViewItem target = serverList.HitTest(p).Item;
            if (target == null)
            {
                Rectangle last = serverList.Items[serverList.Items.Count - 1].Bounds;
                if (p.Y > last.Bottom) { target = serverList.Items[serverList.Items.Count - 1]; }
                else if (serverList.TopItem != null && p.Y < serverList.TopItem.Bounds.Top) { target = serverList.TopItem; }
                else { return; }
            }
            if (drag_items.Contains(target)) { return; }
            bool down = target.Index > drag_items[0].Index;
            serverList.BeginUpdate();
            foreach (ListViewItem item in drag_items) { serverList.Items.Remove(item); }
            int index = target.Index + (down ? 1 : 0);
            foreach (ListViewItem item in drag_items) { serverList.Items.Insert(index++, item); }
            foreach (ListViewItem item in drag_items) { item.Selected = true; }
            serverList.EndUpdate();
            drag_moved = true;
        }

        private void scroll_for_drag(Point p)
        {
            ListViewItem top = serverList.TopItem;
            if (top == null) { return; }
            int band = Math.Max(8, top.Bounds.Height);
            if (p.Y < top.Bounds.Top + band && top.Index > 0) { serverList.EnsureVisible(top.Index - 1); }
            else if (p.Y > serverList.ClientSize.Height - band)
            {
                ListViewItem below = serverList.GetItemAt(4, serverList.ClientSize.Height - 2);
                int next = below != null ? below.Index + 1 : -1;
                if (next > 0 && next < serverList.Items.Count) { serverList.EnsureVisible(next); }
            }
        }

        private void move_selected(int direction)
        {
            List<ListViewItem> items = serverList.SelectedItems.Cast<ListViewItem>().OrderBy(i => i.Index * direction).ToList();
            if (items.Count == 0) { return; }
            int edge = direction < 0 ? items.Min(i => i.Index) : items.Max(i => i.Index);
            if (edge + direction < 0 || edge + direction >= serverList.Items.Count) { return; }
            serverList.BeginUpdate();
            foreach (ListViewItem item in items)
            {
                int index = item.Index;
                serverList.Items.RemoveAt(index);
                serverList.Items.Insert(index + direction, item);
            }
            foreach (ListViewItem item in items) { item.Selected = true; }
            items[0].Focused = true;
            items[0].EnsureVisible();
            serverList.EndUpdate();
            sort_column = -1;
            save_servers();
        }
        private void menuMoveUp_Click(object sender, EventArgs e)
        {
            move_selected(-1);
        }
        private void menuMoveDown_Click(object sender, EventArgs e)
        {
            move_selected(1);
        }

        private ServerEditForm edit_dialog(string title, ServerEntry s, bool all_cores)
        {
            ServerEditForm form = new ServerEditForm();
            form.StartPosition = FormStartPosition.CenterParent;
            form.Text = title;
            form.AllCoresByDefault = all_cores;
            if (s != null)
            {
                form.ServerName = s.Name;
                form.Priority = s.Priority;
                form.HideConsole = s.HideConsole;
                form.ExePath = s.Executable;
                form.LaunchParams = s.Params;
                form.Cores = s.Cores;
                form.RconPassword = s.RconPassword;
            }
            return form;
        }
        private ServerEntry entry_from_dialog(ServerEditForm form)
        {
            ServerEntry s = new ServerEntry();
            s.Name = form.ServerName;
            s.Executable = form.ExePath;
            s.Priority = form.Priority;
            s.HideConsole = form.HideConsole;
            s.Cores = form.Cores;
            s.Params = form.LaunchParams;
            s.RconPassword = form.RconPassword;
            s.Paused = true;
            s.IP = "127.0.0.1";
            return s;
        }

        private readonly ToolStripMenuItem menuAddServer = new ToolStripMenuItem();

        private void menuNewServer_Click(object sender, EventArgs e)
        {
            using (ServerEditForm form = edit_dialog(Lang.T("edit.title_new"), null, true))
            {
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                foreach (ListViewItem lvi in serverList.SelectedItems.Cast<ListViewItem>().ToList()) { lvi.Selected = false; }
                ListViewItem item = add_item(entry_from_dialog(form), -1);
                apply_ip_port(item, form.LaunchParams);
                show(item);
                item.Selected = true;
                save_servers();
            }
        }

        private void menuDuplicateServer_Click(object sender, EventArgs e)
        {
            ListViewItem source = serverList.SelectedItems[0];
            ServerEntry copy = entry_of(source);
            copy.Name = Lang.F("edit.copy_name", copy.Name);
            using (ServerEditForm form = edit_dialog(Lang.T("edit.title_new"), copy, false))
            {
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                ServerEntry s = entry_from_dialog(form);
                s.Schedules = copy.Schedules.Select(x => x.copy()).ToList();
                source.Selected = false;
                ListViewItem item = add_item(s, source.Index + 1);
                apply_ip_port(item, form.LaunchParams);
                show(item);
                item.Selected = true;
                save_servers();
            }
        }

        private void apply_ip_port(ListViewItem item, string launch)
        {
            ServerState st = state_of(item);
            string[] split_param = launch.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split_param.Length - 1; i++)
            {
                string arg = split_param[i], value = split_param[i + 1];
                if (arg == "-ip" || arg == "+ip") { st.IP = value == "0.0.0.0" ? NetQuery.Instance.localhost : value; }
                if (ServerEditForm.PortArgs.Contains(arg)) { st.Port = value; }
            }
        }

        private void menuEditServer_Click(object sender, EventArgs e)
        {
            ListViewItem item = serverList.SelectedItems[0];
            ServerState st = state_of(item);
            using (ServerEditForm form = edit_dialog(Lang.T("edit.title_edit"), entry_of(item), false))
            {
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                st.IP = "127.0.0.1";
                apply_ip_port(item, form.LaunchParams);
                item.Text = form.ServerName;
                st.Exe = form.ExePath;
                st.Params = form.LaunchParams;
                st.RconPassword = form.RconPassword;
                if (st.Pid > 0 && form.Priority != st.Priority) { Core.Instance.process_priority(st.Pid, form.Priority); }
                st.Priority = form.Priority;
                if (st.Pid > 0 && form.Cores != st.Cores) { Core.Instance.process_affinity(st.Pid, form.Cores); }
                st.Cores = form.Cores;
                st.Hidden = form.HideConsole;
                Core.show_window(st.Window, !st.Hidden);
                show(item);
                save_servers();
            }
        }

        private void menuSchedules_Click(object sender, EventArgs e)
        {
            ListViewItem item = serverList.SelectedItems[0];
            using (ScheduleForm form = new ScheduleForm())
            {
                form.StartPosition = FormStartPosition.CenterParent;
                form.Text = Lang.F("sched.window_title", item.Text);
                form.Schedules = state_of(item).Schedules;
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                state_of(item).Schedules = form.Schedules;
                save_servers();
            }
        }

        private void menuDeleteServer_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Lang.T("msg.delete"), Lang.T("dlg.confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { return; }
            foreach (ListViewItem lvi in serverList.SelectedItems.Cast<ListViewItem>().ToList()) { lvi.Remove(); }
            save_servers();
        }

        class Snapshot
        {
            public int Pid;
            public long StartTicks;
            public string Name, Exe, Params, Priority, Cores, IP, Password;
            public int Port;
            public bool Show;
        }
        private Snapshot snapshot(ListViewItem item)
        {
            ServerState st = state_of(item);
            Snapshot s = new Snapshot();
            s.Pid = st.Pid;
            s.StartTicks = st.StartTicks;
            s.Name = item.Text;
            s.Exe = st.Exe;
            s.Params = st.Params;
            s.Priority = st.Priority;
            s.Cores = st.Cores;
            s.Show = !st.Hidden;
            s.IP = st.IP;
            Int32.TryParse(st.Port, out s.Port);
            s.Password = string.IsNullOrEmpty(st.RconPassword) ? Rcon.password_from_params(s.Params) : st.RconPassword;
            return s;
        }

        private void restart_item(ListViewItem item, int shutdown_timeout)
        {
            Snapshot snap = snapshot(item);
            ServerState st = state_of(item);
            unqueue(item);
            if (!is_paused(item)) { set_proc(st, PROC_RESTARTING); set_net(st, ""); }
            clear_process(st);
            clear_query(st);
            st.LastRestart = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            show(item);
            save_servers();
            new Thread(() =>
            {
                Core.Instance.shutdown_process(snap.Pid, snap.StartTicks, snap.Exe, snap.IP, snap.Port, snap.Password, shutdown_timeout);
                NetQuery.Instance.forget(snap.IP, snap.Port);
                Core.StartResult started = Core.Instance.start_process(snap.Exe, snap.Params, snap.Priority, snap.Cores, snap.Show, snap.Name);
                if (started.Pid < 1) { log(Lang.F("log.start_failed", snap.Name, snap.Exe)); }
                try
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        if (item.ListView == null) { pump_restarts(); return; }
                        st.Pid = started.Pid;
                        st.StartTicks = started.StartTicks;
                        st.Window = started.Window;
                        st.LaunchedAt = DateTime.Now;
                        if (!is_paused(item))
                        {
                            set_net(st, "");
                            if (started.Pid > 0) { set_proc(st, PROC_NORMAL); } else { set_proc(st, PROC_LOST, 1, Core.Instance.opt_process_retries); }
                        }
                        show(item);
                        save_servers();
                        pump_restarts();
                    });
                }
                catch { }
            }).Start();
        }
        private void stop_item(ListViewItem item)
        {
            Snapshot snap = snapshot(item);
            set_item_state(item, false);
            clear_process(state_of(item));
            show(item);
            int timeout = Core.Instance.opt_shutdown;
            new Thread(() => Core.Instance.shutdown_process(snap.Pid, snap.StartTicks, snap.Exe, snap.IP, snap.Port, snap.Password, timeout)).Start();
        }

        private void start_item(ListViewItem item)
        {
            if (is_paused(item)) { set_item_state(item, true); }
            if (state_of(item).Pid == 0 && port_free_or_pause(item)) { restart_item(item, 0); }
            show(item);
        }

        private void menuRestartServer_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Lang.T("msg.restart"), Lang.T("dlg.warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { return; }
            foreach (ListViewItem item in serverList.SelectedItems.Cast<ListViewItem>().ToList())
            {
                state_of(item).cancel_active();
                if (!confirm_port_free(item)) { continue; }
                restart_item(item, Core.Instance.opt_shutdown);
            }
        }

        private void menuCloseServer_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Lang.T("msg.close"), Lang.T("dlg.warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { return; }
            foreach (ListViewItem item in serverList.SelectedItems.Cast<ListViewItem>().ToList()) { state_of(item).cancel_active(); stop_item(item); }
            save_servers();
        }

        private void run_schedules()
        {
            bool warn = Core.Instance.opt_warn_rcon && !Core.Instance.opt_wait_empty;
            DateTime target = DateTime.Now.AddSeconds(warn ? warn_lookahead : 0);
            if (target <= schedule_horizon) { return; }
            DateTime from = schedule_horizon;
            schedule_horizon = target;
            foreach (ListViewItem item in serverList.Items)
            {
                ServerState st = state_of(item);
                foreach (ScheduleEntry s in st.Schedules)
                {
                    DateTime at;
                    if (!s.Next(from, target, out at)) { continue; }
                    if (st.Queue.Any(q => q.Due == at && q.Action == s.Action)) { continue; }
                    PendingAction pending = new PendingAction();
                    pending.Action = s.Action;
                    pending.Due = at;
                    pending.Warn = warn && s.Action != ACTION_START;
                    st.Queue.Add(pending);
                    st.Queue.Sort((a, b) => a.Due.CompareTo(b.Due));
                }
            }
        }

        private static bool server_idle(ServerState st, DateTime now)
        {
            if (st.Pid == 0 || st.Net != NET_NORMAL) { return true; }
            return st.EmptySince != DateTime.MinValue && (now - st.EmptySince).TotalMinutes >= empty_minutes;
        }

        private void run_pending(ListViewItem item)
        {
            ServerState st = state_of(item);
            if (st.Queue.Count == 0) { return; }
            DateTime now = DateTime.Now;
            while (st.Queue.Count > 1 && st.Queue[0].Waiting && st.Queue[1].Due <= now) { st.Queue.RemoveAt(0); }
            foreach (PendingAction q in st.Queue.ToList()) { if (q.Warn && now < q.Due) { count_down(item, q, (int)Math.Ceiling((q.Due - now).TotalSeconds)); } }
            PendingAction p = st.Queue[0];
            if (p.Action == ACTION_STOP && is_paused(item) && st.Pid == 0 && !p.Started) { st.Queue.Remove(p); return; }
            if (now < p.Due) { return; }
            string action = p.Action.ToLowerInvariant();
            if (p.Action != ACTION_START && Core.Instance.opt_wait_empty)
            {
                if (Core.Instance.opt_skip_busy)
                {
                    if (st.Pid > 0 && st.Net == NET_NORMAL && st.Humans > 0)
                    {
                        st.Queue.Remove(p);
                        log(Lang.F("log.skipped_" + action, item.Text));
                        show(item);
                        return;
                    }
                }
                else if (!server_idle(st, now))
                {
                    if (!p.Waiting) { p.Waiting = true; log(Lang.F("log.waiting_" + action, item.Text, empty_minutes)); show(item); }
                    return;
                }
            }
            st.Queue.Remove(p);
            if (p.Warn) { send_say(item, Lang.T("rcon." + action + "_now")); }
            log_event(Core.NOTIFY_SCHEDULE, Lang.F("log.scheduled_" + action, item.Text));
            if (p.Action == ACTION_START) { start_item(item); save_servers(); }
            else if (p.Action == ACTION_STOP) { stop_item(item); save_servers(); }
            else if (port_free_or_pause(item)) { restart_item(item, Core.Instance.opt_shutdown); }
            show(item);
        }

        private void count_down(ListViewItem item, PendingAction p, int remaining)
        {
            if (remaining > warning_seconds[0]) { return; }
            string action = p.Action.ToLowerInvariant();
            if (!p.Started)
            {
                p.Started = true;
                log(Lang.F("log.countdown_" + action, item.Text, p.Due.ToString("HH:mm")));
                if (snapshot(item).Password.Length == 0) { log(Lang.F("log.warn_no_rcon", item.Text)); }
                show(item);
            }
            int[] due = warning_seconds.Where(t => t >= remaining && t < p.LastWarning).ToArray();
            if (due.Length == 0) { return; }
            int next = due.Min();
            p.LastWarning = next;
            if (next - remaining > 2) { return; }
            string prefix = "rcon." + action;
            string text = next >= 60 ? (next == 60 ? Lang.T(prefix + "_minute") : Lang.F(prefix + "_minutes", next / 60)) : Lang.F(prefix + "_seconds", next);
            send_say(item, text);
        }

        private void send_say(ListViewItem item, string text)
        {
            Snapshot snap = snapshot(item);
            if (snap.Password.Length == 0) { return; }
            ThreadPool.QueueUserWorkItem(_ => Rcon.say(snap.IP, snap.Port, snap.Password, text));
        }

        private bool track_processes(List<ListViewItem> items, Dictionary<int, Core.ProcInfo> procs)
        {
            bool changed = false;
            HashSet<int> claimed = new HashSet<int>();
            foreach (ListViewItem item in items)
            {
                ServerState st = state_of(item);
                if (st.Pid == 0) { continue; }
                Core.ProcInfo p;
                if (procs.TryGetValue(st.Pid, out p) && Core.same_path(p.Path, st.Exe) && (st.StartTicks == 0 || p.StartTicks == st.StartTicks))
                {
                    st.StartTicks = p.StartTicks;
                    claimed.Add(st.Pid);
                }
                else
                {
                    clear_process(st);
                    changed = true;
                }
            }
            List<NetQuery.UdpOwner> udp = null;
            foreach (ListViewItem item in items)
            {
                ServerState st = state_of(item);
                if (st.Pid == 0 && st.Proc != PROC_RESTARTING)
                {
                    List<Core.ProcInfo> candidates = procs.Values.Where(p => !claimed.Contains(p.Pid) && Core.same_path(p.Path, st.Exe)).ToList();
                    if (candidates.Count > 0)
                    {
                        Core.ProcInfo pick = null;
                        int port;
                        if (Int32.TryParse(st.Port.Trim(), out port))
                        {
                            if (udp == null) { udp = NetQuery.Instance.udp_table(); }
                            int owner = NetQuery.port_owner(udp, st.IP, port);
                            pick = candidates.FirstOrDefault(p => p.Pid == owner);
                        }
                        if (pick == null && !ServerEditForm.is_cs2(st.Exe) && candidates.Count == 1 &&items.Count(i => Core.same_path(state_of(i).Exe, st.Exe)) == 1) { pick = candidates[0]; }
                        if (pick != null)
                        {
                            st.Pid = pick.Pid;
                            st.StartTicks = pick.StartTicks;
                            st.Window = 0;
                            claimed.Add(pick.Pid);
                            changed = true;
                        }
                    }
                }
                if (st.Pid > 0 && !Core.window_of(st.Window, st.Pid))
                {
                    st.Window = Core.find_window(st.Pid);
                    if (st.Window != 0)
                    {
                        if (st.Hidden) { Core.show_window(st.Window, false); }
                        changed = true;
                    }
                }
                if (st.Pid > 0) { Core.set_title(st.Window, item.Text); }
            }
            return changed;
        }

        private void checkTimer_Tick(object sender, EventArgs e)
        {
            run_schedules();
            List<ListViewItem> items = serverList.Items.Cast<ListViewItem>().ToList();
            bool changed = track_processes(items, Core.snapshot(items.Select(i => state_of(i).Exe)));
            List<ListViewItem> net_targets = new List<ListViewItem>();
            foreach (ListViewItem item in items)
            {
                ServerState st = state_of(item);
                if (st.Proc != PROC_RESTARTING && st.Proc != PROC_PAUSED)
                {
                    if (st.Pid > 0)
                    {
                        if (st.Proc != PROC_NORMAL) { set_proc(st, PROC_NORMAL); }
                        net_targets.Add(item);
                    }
                    else { process_lost(item); }
                }
                show(item);
            }
            if (changed) { save_servers(); }
            pump_restarts();
            fill_last_column();
            if (!net_busy && net_targets.Count > 0) { start_network_checks(net_targets); }
            if (service_mode) { write_status(); }
        }

        private void process_lost(ListViewItem item)
        {
            ServerState st = state_of(item);
            int max = Core.Instance.opt_process_retries;
            int count = st.Proc == PROC_LOST ? st.ProcCount + 1 : 1;
            if (count == 1) { set_net(st, ""); clear_query(st); }
            set_proc(st, PROC_LOST, Math.Min(count, max), max);
            if (count >= max) { queue_restart(item, Lang.T("reason.process_lost")); }
        }

        private void start_network_checks(List<ListViewItem> targets)
        {
            net_busy = true;
            bool count_hltv = Core.Instance.opt_exclude_hltv || Core.Instance.opt_wait_empty;
            List<NetJob> jobs = targets.Select(item =>
            {
                Snapshot snap = snapshot(item);
                NetJob job = new NetJob();
                job.Item = item;
                job.IP = snap.IP;
                job.Port = snap.Port;
                job.Password = snap.Password;
                job.HltvKey = state_of(item).HltvKey;
                job.Hltv = state_of(item).HltvCount;
                job.HltvKnown = state_of(item).HltvKnown;
                return job;
            }).ToList();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { Parallel.ForEach(jobs, job => query_server(job, count_hltv)); }
                catch { }
                try { this.BeginInvoke((MethodInvoker)delegate { net_busy = false; apply_network_results(jobs); }); }
                catch { net_busy = false; }
            });
        }

        private static void query_server(NetJob job, bool count_hltv)
        {
            job.Result = NetQuery.Instance.a2sinfo(job.IP, job.Port);
            if (!job.Result.Ok) { return; }
            if (job.Result.SourceTv) { job.Hltv = 1; return; }
            if (!count_hltv || job.Result.Players == 0) { job.Hltv = 0; job.HltvKey = null; return; }
            List<string> names = NetQuery.Instance.a2splayers(job.IP, job.Port);
            if (names == null) { return; }
            string key = string.Join("\n", names.OrderBy(n => n, StringComparer.Ordinal));
            if (key == job.HltvKey) { return; }
            bool goldsrc = job.Password.Length > 0 && Rcon.is_goldsrc(job.IP, job.Port);
            string status = goldsrc ? Rcon.query_goldsrc(job.IP, job.Port, job.Password, "status") : null;
            if (status != null && status.Contains("#"))
            {
                job.Hltv = Rcon.count_hltv(status);
                job.HltvKey = key;
                job.HltvKnown = true;
                return;
            }
            bool failed = goldsrc && status == null;
            if (failed && job.HltvKnown) { job.HltvKey = null; return; }
            job.Hltv = names.Count(n => n.IndexOf("HLTV", StringComparison.OrdinalIgnoreCase) >= 0);
            job.HltvKey = failed ? null : key;
        }

        private void apply_network_results(List<NetJob> jobs)
        {
            foreach (NetJob job in jobs)
            {
                ListViewItem item = job.Item;
                ServerState st = state_of(item);
                if (item.ListView == null || job.Result == null || st.Proc != PROC_NORMAL) { continue; }
                if (job.Result.Ok)
                {
                    set_net(st, NET_NORMAL);
                    int bots = job.Result.SourceTv ? Math.Max(0, job.Result.Bots - 1) : job.Result.Bots;
                    int players = job.Result.Players;
                    st.Humans = Math.Max(0, players - bots - job.Hltv);
                    if (st.Humans > 0) { st.EmptySince = DateTime.MinValue; }
                    else if (st.EmptySince == DateTime.MinValue) { st.EmptySince = DateTime.Now; }
                    if (Core.Instance.opt_exclude_bots) { players -= bots; }
                    if (Core.Instance.opt_exclude_hltv) { players -= job.Hltv; }
                    st.Info = job.Result;
                    st.HltvKey = job.HltvKey;
                    st.HltvCount = job.Hltv;
                    st.HltvKnown = job.HltvKnown;
                    st.Map = job.Result.Map;
                    st.Players = Math.Max(0, players);
                    st.MaxPlayers = job.Result.MaxPlayers;
                }
                else
                {
                    int max = Core.Instance.opt_network_retries;
                    int count = st.Net == NET_TIMEOUT ? st.NetCount + 1 : 1;
                    set_net(st, NET_TIMEOUT, Math.Min(count, max), max);
                    if (count >= max) { queue_restart(item, Lang.T("reason.no_network")); }
                }
                show(item);
            }
            pump_restarts();
        }

        private void recover_server(ListViewItem item, string reason)
        {
            ServerState st = state_of(item);
            List<DateTime> restarts = st.RestartTimes;
            DateTime window_start = DateTime.Now.AddMinutes(-crash_loop_minutes);
            restarts.RemoveAll(t => t < window_start);
            if (Core.Instance.opt_loop_count > 0 && restarts.Count >= Core.Instance.opt_loop_count)
            {
                log_event(Core.NOTIFY_CRASH_LOOP, Lang.F("log.crash_loop", item.Text, restarts.Count, crash_loop_minutes));
                set_item_state(item, false);
                show(item);
                save_servers();
                return;
            }
            if (!port_free_or_pause(item)) { return; }
            restarts.Add(DateTime.Now);
            st.Restarts++;
            log_event(Core.NOTIFY_RESTART, Lang.F("log.restarting", item.Text, reason));
            restart_item(item, 0);
        }

        private static bool starting(ServerState st)
        {
            if (st.Proc == PROC_RESTARTING) { return true; }
            return st.Proc == PROC_NORMAL && st.Net != NET_NORMAL && (DateTime.Now - st.LaunchedAt).TotalSeconds < answer_timeout_seconds;
        }

        private void queue_restart(ListViewItem item, string reason)
        {
            if (restart_queue.Any(q => q.Key == item)) { return; }
            restart_queue.Add(new KeyValuePair<ListViewItem, string>(item, reason));
            state_of(item).Queued = true;
            pump_restarts();
            int ahead = restart_queue.FindIndex(q => q.Key == item);
            if (ahead >= 0)
            {
                log(Lang.F("log.restart_queued", item.Text, ahead));
                show(item);
            }
        }

        private void unqueue(ListViewItem item)
        {
            restart_queue.RemoveAll(q => q.Key == item);
            state_of(item).Queued = false;
        }

        private void pump_restarts()
        {
            while (restart_queue.Count > 0 && serverList.Items.Cast<ListViewItem>().Count(i => starting(state_of(i))) < (Core.Instance.opt_max_restarts > 0 ? Core.Instance.opt_max_restarts : auto_restart_slots))
            {
                KeyValuePair<ListViewItem, string> next = restart_queue[0];
                restart_queue.RemoveAt(0);
                ListViewItem item = next.Key;
                ServerState st = state_of(item);
                st.Queued = false;
                if (item.ListView == null || is_paused(item)) { continue; }
                if (st.Proc != PROC_LOST && st.Net != NET_TIMEOUT)
                {
                    log(Lang.F("log.queue_recovered", item.Text));
                    show(item);
                    continue;
                }
                recover_server(item, next.Value);
            }
        }

        private void write_status()
        {
            StatusFile status = new StatusFile();
            status.Updated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            List<ColumnHeader> columns = serverList.Columns.Cast<ColumnHeader>().Where(c => column_visible[c.Index]).OrderBy(c => c.DisplayIndex).ToList();
            status.Columns = columns.Select(c => c.Text).ToArray();
            status.Rows = serverList.Items.Cast<ListViewItem>().Select(i => columns.Select(c => i.SubItems[c.Index].Text).ToArray()).ToList();
            try { Json.Save(Core.status_path, status); }
            catch { }
        }
    }
}
