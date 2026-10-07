using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace hlsmx
{
    static class Program
    {
        public static string version
        {
            get
            {
                Version v = typeof(Program).Assembly.GetName().Version;
                return v.Build > 0 ? string.Format("{0}.{1}.{2}", v.Major, v.Minor, v.Build) : string.Format("{0}.{1}", v.Major, v.Minor);
            }
        }

        public static DateTime build_date
        {
            get
            {
                string path = typeof(Program).Assembly.Location;
                try
                {
                    byte[] header = new byte[2048];
                    using (System.IO.FileStream fs = System.IO.File.OpenRead(path)) { fs.Read(header, 0, header.Length); }
                    int pe = BitConverter.ToInt32(header, 0x3C);
                    int seconds = BitConverter.ToInt32(header, pe + 8);
                    DateTime stamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(seconds).ToLocalTime();
                    if (stamp.Year >= 2020 && stamp <= DateTime.Now.AddDays(1)) { return stamp; }
                }
                catch { }
                return System.IO.File.GetLastWriteTime(path);
            }
        }

        public static string version_with_date
        {
            get { return string.Format("{0} ({1})", version, build_date.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture)); }
        }
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int RegisterWindowMessage(string message);

        public static readonly int WM_SHOWME = RegisterWindowMessage("HLSMX_SHOWME");

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Any(a => a.Equals("-service", StringComparison.OrdinalIgnoreCase)))
            {
                ServiceBase.Run(new HlsmxService());
                return;
            }

            bool tray = args.Any(a => a.Equals("-tray", StringComparison.OrdinalIgnoreCase));
            bool created;
            using (Mutex mutex = new Mutex(true, @"Local\hlsmx_single_instance", out created))
            {
                if (!created)
                {
                    PostMessage((IntPtr)0xffff, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                    return;
                }
                Application.SetCompatibleTextRenderingDefault(false);
                Application.EnableVisualStyles();
                Mutex global = null;
                bool global_created;
                try { global = new Mutex(true, @"Global\hlsmx_single_instance", out global_created); }
                catch (UnauthorizedAccessException) { global_created = false; }
                using (global)
                {
                    if (!global_created)
                    {
                        if (tray) { return; }
                        Lang.load(Lang.resolve(Core.Instance.peek_language()));
                        MessageBox.Show(Lang.T("msg.running_elsewhere"), "HLSMX", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    run(tray);
                }
            }
        }

        static void run(bool tray)
        {
            if (ServiceHelper.Running)
            {
                Lang.load(Lang.resolve(Core.Instance.peek_language()));
                if (tray) { return; }
                using (StatusForm status = new StatusForm())
                {
                    Application.Run(status);
                    if (!status.Manage) { return; }
                }
                ServiceHelper.Stop();
                if (ServiceHelper.Running)
                {
                    MessageBox.Show(Lang.T("svc.stop_failed"), "HLSMX", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            Application.Run(new MainForm(tray, false));
        }
    }
}
