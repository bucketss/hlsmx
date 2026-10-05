using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace hlsmx
{
    class StatusForm : Form
    {
        private readonly Label info = new Label();
        private readonly ListView list = new ListView();
        private readonly TextBox log_box = new TextBox();
        private readonly Timer timer = new Timer();
        private string[] shown_columns = new string[0];
        private string last_log = null;

        public bool Manage { get; private set; }

        public StatusForm()
        {
            Text = Lang.T("app.title") + " - " + Lang.T("svcview.title");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 520);
            MinimumSize = new Size(480, 320);
            Icon = Core.Instance.app_icon;

            info.AutoSize = false;
            info.SetBounds(12, 10, ClientSize.Width - 24, 20);
            info.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            list.View = View.Details;
            list.FullRowSelect = true;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.SetBounds(12, 34, ClientSize.Width - 24, 260);
            list.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;

            Label log_label = new Label { Text = Lang.T("svcview.log"), AutoSize = true, Location = new Point(12, 302) };
            log_label.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            log_box.Multiline = true;
            log_box.ReadOnly = true;
            log_box.ScrollBars = ScrollBars.Vertical;
            log_box.SetBounds(12, 320, ClientSize.Width - 24, 150);
            log_box.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            Button manage = new Button { Text = Lang.T("svcview.manage") };
            Button close = new Button { Text = Lang.T("svcview.close"), DialogResult = DialogResult.Cancel };
            foreach (Button b in new[] { manage, close })
            {
                b.Size = new Size(Math.Max(75, TextRenderer.MeasureText(b.Text, b.Font).Width + 20), 25);
                b.Top = ClientSize.Height - 37;
                b.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            }
            close.Left = ClientSize.Width - 12 - close.Width;
            manage.Left = close.Left - 6 - manage.Width;
            manage.Click += (s, e) => { Manage = true; Close(); };
            CancelButton = close;

            Controls.AddRange(new Control[] { info, list, log_label, log_box, manage, close });
            Theme.Apply(this);

            timer.Interval = 2000;
            timer.Tick += (s, e) => refresh();
            Shown += (s, e) => { refresh(); timer.Start(); };
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.WM_SHOWME)
            {
                if (WindowState == FormWindowState.Minimized) { WindowState = FormWindowState.Normal; }
                Activate();
            }
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); }
            base.Dispose(disposing);
        }

        private void refresh()
        {
            StatusFile status = null;
            try { if (File.Exists(Core.status_path)) { status = Json.Load<StatusFile>(Core.status_path); } }
            catch { }
            if (status == null || status.Columns == null || status.Rows == null)
            {
                info.Text = Lang.T("svcview.no_data");
            }
            else
            {
                DateTime updated;
                bool stale = !DateTime.TryParse(status.Updated, out updated) || (DateTime.Now - updated).TotalSeconds > 90;
                info.Text = Lang.F(stale ? "svcview.stale" : "svcview.updated", status.Updated);
                show_rows(status);
            }
            show_log();
        }

        private void show_rows(StatusFile status)
        {
            list.BeginUpdate();
            if (!status.Columns.SequenceEqual(shown_columns))
            {
                list.Columns.Clear();
                foreach (string column in status.Columns) { list.Columns.Add(column, 100); }
                shown_columns = status.Columns;
            }
            while (list.Items.Count > status.Rows.Count) { list.Items.RemoveAt(list.Items.Count - 1); }
            for (int r = 0; r < status.Rows.Count; r++)
            {
                string[] row = status.Rows[r];
                if (r >= list.Items.Count) { list.Items.Add(new ListViewItem(Enumerable.Repeat("", shown_columns.Length).ToArray())); }
                ListViewItem item = list.Items[r];
                for (int c = 0; c < shown_columns.Length && c < row.Length; c++)
                {
                    if (item.SubItems[c].Text != row[c]) { item.SubItems[c].Text = row[c]; }
                }
            }
            if (list.Items.Count > 0 && list.Tag == null)
            {
                list.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                foreach (ColumnHeader column in list.Columns) { column.Width = Math.Max(column.Width, TextRenderer.MeasureText(column.Text, list.Font).Width + 16); }
                list.Tag = true;
            }
            list.EndUpdate();
        }

        private void show_log()
        {
            string text = read_tail(Path.Combine(Core.logs_dir, DateTime.Now.ToString("yyyy-MM") + ".log"), 32768);
            if (text == last_log) { return; }
            last_log = text;
            log_box.Text = text;
            log_box.SelectionStart = log_box.TextLength;
            log_box.ScrollToCaret();
        }

        private static string read_tail(string path, int bytes)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0, fs.Length - bytes);
                    fs.Seek(start, SeekOrigin.Begin);
                    byte[] data = new byte[fs.Length - start];
                    int read = fs.Read(data, 0, data.Length);
                    string text = Encoding.UTF8.GetString(data, 0, read).TrimEnd();
                    if (start > 0)
                    {
                        int line = text.IndexOf('\n');
                        text = line < 0 ? "" : text.Substring(line + 1);
                    }
                    return text;
                }
            }
            catch { return ""; }
        }
    }
}
