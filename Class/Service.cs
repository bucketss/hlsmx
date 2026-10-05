using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace hlsmx
{
    class HlsmxService : ServiceBase
    {
        private Thread ui_thread;
        private MainForm form;

        public HlsmxService()
        {
            ServiceName = ServiceHelper.Name;
        }

        protected override void OnStart(string[] args)
        {
            ManualResetEvent ready = new ManualResetEvent(false);
            ui_thread = new Thread(() =>
            {
                form = new MainForm(true, true);
                ready.Set();
                Application.Run(form);
            });
            ui_thread.SetApartmentState(ApartmentState.STA);
            ui_thread.Start();
            ready.WaitOne(30000);
        }

        protected override void OnStop()
        {
            if (form != null && form.IsHandleCreated) { form.BeginInvoke((MethodInvoker)delegate { form.ExitApp(); }); }
            if (ui_thread != null) { ui_thread.Join(15000); }
        }
    }

    static class ServiceHelper
    {
        public const string Name = "hlsmx";

        public static string Status()
        {
            try
            {
                using (ServiceController sc = new ServiceController(Name))
                {
                    return sc.Status.ToString();
                }
            }
            catch { return "Not installed"; }
        }

        public static bool Installed { get { return Status() != "Not installed"; } }
        public static bool Running { get { return Status() == "Running"; } }

        public static bool Install()
        {
            string exe = Application.ExecutablePath;
            return run_elevated(
                "sc.exe create " + Name + " binPath= \"\\\"" + exe + "\\\" -service\" start= auto DisplayName= \"HL Server Monitor X\"",
                "sc.exe description " + Name + " \"Monitors and restarts Half-Life dedicated servers.\"",
                "sc.exe start " + Name);
        }

        public static bool Uninstall()
        {
            return run_elevated("sc.exe stop " + Name, "ping -n 3 127.0.0.1 >nul", "sc.exe delete " + Name);
        }

        public static bool Restart()
        {
            return run_elevated("sc.exe stop " + Name, "ping -n 3 127.0.0.1 >nul", "sc.exe start " + Name);
        }

        public static bool Stop()
        {
            return run_elevated("sc.exe stop " + Name, "ping -n 3 127.0.0.1 >nul");
        }

        private static bool run_elevated(params string[] commands)
        {
            string script = Path.Combine(Path.GetTempPath(), "hlsmx_service.cmd");
            File.WriteAllText(script, "@echo off\r\n" + string.Join("\r\n", commands) + "\r\n", Encoding.Default);
            ProcessStartInfo info = new ProcessStartInfo(script);
            info.Verb = "runas";
            info.UseShellExecute = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            try
            {
                using (Process p = Process.Start(info)) { p.WaitForExit(30000); }
                return true;
            }
            catch (Win32Exception) { return false; }
            finally
            {
                try { File.Delete(script); } catch { }
            }
        }
    }
}
