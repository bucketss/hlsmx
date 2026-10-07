using System;
using System.Drawing;
using System.Windows.Forms;

namespace hlsmx
{
    class WebhookForm : Form
    {
        private readonly TextBox name = new TextBox();
        private readonly TextBox url = new TextBox();
        private readonly Label detected = new Label();
        private readonly ComboBox type = new ComboBox();
        private readonly Button test = new Button();
        private readonly CheckBox[] events = { new CheckBox(), new CheckBox(), new CheckBox(), new CheckBox() };
        private static readonly string[] event_keys = { "opt.notify_start", "opt.notify_restart", "opt.notify_crash_loop", "opt.notify_schedule" };

        public WebhookSetting Result { get; private set; }

        public WebhookForm(WebhookSetting hook)
        {
            bool adding = hook == null;
            hook = hook == null ? new WebhookSetting() : hook.copy();
            Text = Lang.T(adding ? "hook.title_add" : "hook.title_edit");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            Icon = Core.Instance.app_icon;

            Label name_label = new Label { Text = Lang.T("edit.name"), AutoSize = true };
            Label url_label = new Label { Text = Lang.T("hook.url"), AutoSize = true };
            Label type_label = new Label { Text = Lang.T("hook.type"), AutoSize = true };
            Label send_label = new Label { Text = Lang.T("hook.send"), AutoSize = true };
            test.Text = Lang.T("opt.test");
            Button ok = new Button { Text = Lang.T("dlg.ok") };
            Button cancel = new Button { Text = Lang.T("dlg.cancel"), DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { name_label, name, url_label, url, detected, type_label, type, send_label, test, ok, cancel });
            for (int i = 0; i < events.Length; i++)
            {
                events[i].Text = Lang.T(event_keys[i]);
                events[i].AutoSize = true;
                Controls.Add(events[i]);
            }

            int field = Math.Max(name_label.PreferredWidth, Math.Max(url_label.PreferredWidth, Math.Max(type_label.PreferredWidth, send_label.PreferredWidth))) + 24;
            int width = Math.Max(380, field + 260);
            name_label.Location = new Point(12, 15);
            name.SetBounds(field, 12, width - field - 12, 21);
            url_label.Location = new Point(12, 44);
            url.SetBounds(field, 41, width - field - 12, 21);
            detected.AutoSize = true;
            detected.Location = new Point(field, 67);
            type_label.Location = new Point(12, 92);
            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.SetBounds(field, 88, 140, 21);
            foreach (string t in Core.WebhookTypes) { type.Items.Add(type_text(t)); }
            type.SelectedIndex = Math.Max(0, Array.IndexOf(Core.WebhookTypes, hook.Type));
            send_label.Location = new Point(12, 121);
            for (int i = 0; i < events.Length; i++) { events[i].Location = new Point(field, 119 + i * 22); }
            int bottom = events[events.Length - 1].Top + 34;
            foreach (Button b in new[] { test, ok, cancel }) { b.Size = new Size(Math.Max(75, TextRenderer.MeasureText(b.Text, b.Font).Width + 20), 25); b.Top = bottom; }
            test.Left = 12;
            cancel.Left = width - 12 - cancel.Width;
            ok.Left = cancel.Left - 6 - ok.Width;
            ClientSize = new Size(width, bottom + 37);
            AcceptButton = ok;
            CancelButton = cancel;

            name.Text = hook.Name;
            url.Text = hook.Url;
            events[0].Checked = hook.NotifyStart;
            events[1].Checked = hook.NotifyRestart;
            events[2].Checked = hook.NotifyCrashLoop;
            events[3].Checked = hook.NotifySchedule;
            url.TextChanged += (s, e) => refresh_detected();
            type.SelectedIndexChanged += (s, e) => refresh_detected();
            refresh_detected();

            test.Click += (s, e) => send_test();
            ok.Click += (s, e) => accept();
            Theme.Apply(this);
        }

        private string selected_type { get { return Core.WebhookTypes[Math.Max(0, type.SelectedIndex)]; } }

        private static string type_text(string t)
        {
            if (t.Length == 0) { return Lang.T("hook.type_auto"); }
            return t == Core.WEBHOOK_TYPE_GENERIC ? Lang.T("webhook.kind_generic") : t;
        }

        private void refresh_detected()
        {
            detected.Text = url.Text.Trim().Length == 0 || selected_type.Length > 0 ? "" : Lang.F("webhook.detected", Core.webhook_kind_name(url.Text, ""));
        }

        private bool url_valid()
        {
            if (Core.webhook_kind(url.Text, selected_type) != null) { return true; }
            MessageBox.Show(this, Lang.T("webhook.bad_url"), Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            url.Focus();
            return false;
        }

        private void send_test()
        {
            if (!url_valid()) { return; }
            string sent = url.Text.Trim();
            string type = selected_type;
            string message = Lang.T("webhook.test_message");
            test.Enabled = false;
            Cursor = Cursors.AppStarting;
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string resolved;
                string error = Core.Instance.post_webhook(sent, type, message, out resolved);
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (IsDisposed) { return; }
                        Cursor = Cursors.Default;
                        test.Enabled = true;
                        if (resolved != null && url.Text.Trim() == sent) { url.Text = resolved; }
                        if (error == null) { MessageBox.Show(this, Lang.T("webhook.sent"), Lang.T("webhook.title"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
                        else { MessageBox.Show(this, Lang.F("webhook.failed", error), Lang.T("webhook.title"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        private void accept()
        {
            if (!url_valid()) { return; }
            WebhookSetting hook = new WebhookSetting();
            hook.Name = name.Text.Trim();
            hook.Url = url.Text.Trim();
            hook.Type = selected_type;
            hook.NotifyStart = events[0].Checked;
            hook.NotifyRestart = events[1].Checked;
            hook.NotifyCrashLoop = events[2].Checked;
            hook.NotifySchedule = events[3].Checked;
            Result = hook;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
