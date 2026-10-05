using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace hlsmx
{
    class AboutForm : Form
    {
        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr instance, IntPtr name, uint type, int width, int height, uint flags);
        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr icon);

        private readonly Panel logo = new Panel();
        private IntPtr logo_handle;
        private Icon logo_icon;

        public AboutForm()
        {
            Text = Lang.T("about.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Icon = Core.Instance.app_icon;

            logo.Size = new Size(48, 48);
            logo.Location = new Point(20, 20);
            logo_handle = LoadImage(Marshal.GetHINSTANCE(typeof(AboutForm).Module), (IntPtr)32512, 1, 48, 48, 0);
            logo_icon = logo_handle != IntPtr.Zero ? Icon.FromHandle(logo_handle) : Core.Instance.app_icon;
            logo.Paint += (s, e) => e.Graphics.DrawIcon(logo_icon, new Rectangle(0, 0, logo.Width, logo.Height));

            Label text = new Label { Text = Lang.F("about.text", Program.version_with_date), AutoSize = true, Location = new Point(84, 22) };
            Button ok = new Button { Text = Lang.T("dlg.ok"), DialogResult = DialogResult.OK };
            ok.Size = new Size(Math.Max(75, TextRenderer.MeasureText(ok.Text, ok.Font).Width + 20), 25);
            Controls.AddRange(new Control[] { logo, text, ok });

            int width = Math.Max(320, text.Left + text.PreferredWidth + 24);
            int top = Math.Max(logo.Bottom, text.Top + text.PreferredHeight) + 18;
            ok.Location = new Point(width - 12 - ok.Width, top);
            ClientSize = new Size(width, ok.Bottom + 12);
            AcceptButton = ok;
            CancelButton = ok;
            Theme.Apply(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && logo_icon != null) { logo_icon.Dispose(); }
            if (logo_handle != IntPtr.Zero) { DestroyIcon(logo_handle); logo_handle = IntPtr.Zero; }
            base.Dispose(disposing);
        }
    }
}
