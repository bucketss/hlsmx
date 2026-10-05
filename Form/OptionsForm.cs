using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace hlsmx
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();
            this.Icon = Core.Instance.app_icon;
            Text = Lang.T("opt.title");
            tabGeneral.Text = Lang.T("opt.tab_general");
            tabAppearance.Text = Lang.T("opt.tab_appearance");
            tabNotifications.Text = Lang.T("opt.tab_notifications");
            tabService.Text = Lang.T("opt.tab_service");
            labelInterval.Text = Lang.T("opt.check_interval");
            labelProcessRetries.Text = Lang.T("opt.process_retries");
            labelNetworkRetries.Text = Lang.T("opt.network_retries");
            labelMaxRestarts.Text = Lang.T("opt.max_restarts");
            labelCrashLimit.Text = Lang.T("opt.loop_count");
            labelShutdown.Text = Lang.T("opt.shutdown");
            checkTray.Text = Lang.T("opt.tray");
            checkBoxLocalIps.Text = Lang.T("opt.local_ips");
            buttonImport.Text = Lang.T("opt.import");
            labelLanguage.Text = Lang.T("opt.language");
            labelLanguage.AutoSize = true;
            labelLanguage.Location = new Point(labelIcon.Left, labelIcon.Top + (labelIcon.Top - labelTheme.Top));
            comboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            comboLanguage.Size = comboIcon.Size;
            labelTheme.Text = Lang.T("opt.theme");
            labelIcon.Text = Lang.T("opt.icon");
            buttonHookAdd.Text = Lang.T("opt.hook_add");
            buttonHookEdit.Text = Lang.T("opt.hook_edit");
            buttonHookRemove.Text = Lang.T("opt.hook_remove");
            buttonTest.Text = Lang.T("opt.test");
            labelHookHint.Text = Lang.T("opt.webhook_hint");
            listViewHooks.Columns.Add(Lang.T("col.name"), 100);
            listViewHooks.Columns.Add(Lang.T("hook.col_type"), 70);
            listViewHooks.Columns.Add(Lang.T("hook.col_events"), 120);
            listViewHooks.ShowItemToolTips = true;
            labelServiceHint.Text = Lang.T("opt.service_hint");
            buttonSave.Text = Lang.T("opt.save");
            buttonApply.Text = Lang.T("opt.apply");
            buttonApply.Size = buttonSave.Size;
            buttonApply.Location = buttonSave.Location;
            buttonApply.Left -= buttonCancel.Left - buttonSave.Left;
            buttonApply.TabIndex = buttonSave.TabIndex;
            buttonSave.TabIndex++;
            buttonCancel.TabIndex++;
            buttonApply.Click += Apply_Click;
            this.Controls.Add(buttonApply);
            buttonCancel.Text = Lang.T("dlg.cancel");
            build_scheduling_tab();
            comboBoxData.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxData.Items.AddRange(new object[] { Lang.T("opt.data_program"), Lang.T("opt.data_appdata") });
            tabGeneral.Controls.Add(comboBoxData);
            comboBoxData.Width = comboBoxData.Items.Cast<object>().Max(i => TextRenderer.MeasureText(i.ToString(), comboBoxData.Font).Width) + SystemInformation.VerticalScrollBarWidth + 12;
            comboBoxData.Location = new Point(checkTray.Left, (numShutdown.Bottom + checkTray.Top - comboBoxData.Height) / 2);
            Theme.Apply(this);
            fit_layout();
            fit_hook_buttons();
        }

        public bool service_installed = false;
        public Action applied;
        private readonly Button buttonApply = new Button();
        private readonly ComboBox comboBoxData = new ComboBox();
        public Action import_configs;
        private List<Lang.Info> languages = new List<Lang.Info>();

        private void fit_layout()
        {
            int field = Math.Max(comboTheme.Left, Math.Max(labelTheme.PreferredWidth, Math.Max(labelIcon.PreferredWidth, labelLanguage.PreferredWidth)) + labelTheme.Left + 8);
            comboTheme.Left = field;
            comboIcon.Left = field;
            comboLanguage.Left = field;
            comboLanguage.Top = labelLanguage.Top - (labelIcon.Top - comboIcon.Top);
            iconPreview.Left = comboIcon.Right + 12;
            checkBoxLocalIps.Left = checkTray.Right + 16;
            buttonImport.Width = Math.Max(60, TextRenderer.MeasureText(buttonImport.Text, buttonImport.Font).Width + 16);
            Label[] row_labels = { labelInterval, labelProcessRetries, labelNetworkRetries, labelMaxRestarts, labelCrashLimit, labelShutdown };
            NumericUpDown[] row_numbers = { numInterval, numProcessRetries, numNetworkRetries, numMaxRestarts, numCrashLimit, numShutdown };
            int page = tabGeneral.ClientSize.Width;
            int margin = page - numInterval.Right;
            int shift = Math.Max(0, row_labels.Max(l => l.Left + l.PreferredWidth) + 8 - numInterval.Left);
            foreach (NumericUpDown number in row_numbers) { number.Left += shift; }
            int min_left = checkBoxLocalIps.Left + checkBoxLocalIps.PreferredSize.Width + 8;
            int overflow = Math.Max(numInterval.Right + margin - page, min_left + buttonImport.Width + 8 - page);
            if (overflow > 0)
            {
                this.Width += overflow;
                tabs.Width += overflow;
                buttonSave.Left += overflow;
                buttonApply.Left += overflow;
                buttonCancel.Left += overflow;
            }
            buttonImport.Left = page + Math.Max(0, overflow) - 8 - buttonImport.Width;
            buttonImport.Top = checkTray.Top + (checkTray.Height - buttonImport.Height) / 2;
            checkBoxBots.Location = new Point(labelTheme.Left + 2, comboLanguage.Bottom + 20);
            checkBoxHltv.Location = new Point(labelTheme.Left + 2, checkBoxBots.Bottom + 6);
        }

        private List<WebhookSetting> hooks = new List<WebhookSetting>();

        private void fit_hook_buttons()
        {
            listViewHooks.Width = tabs.Width - 26;
            labelHookHint.Width = listViewHooks.Width;
            listViewHooks.Columns[0].Width = 90;
            listViewHooks.Columns[1].Width = 85;
            int left = listViewHooks.Left;
            foreach (Button b in new[] { buttonHookAdd, buttonHookEdit, buttonHookRemove, buttonTest })
            {
                b.Width = Math.Max(60, TextRenderer.MeasureText(b.Text, b.Font).Width + 16);
                b.Left = left;
                b.Top = listViewHooks.Bottom + 6;
                left = b.Right + 6;
            }
            labelHookHint.Top = buttonTest.Bottom + 6;
            labelHookHint.Height = tabNotifications.ClientSize.Height - labelHookHint.Top - 2;
        }

        private readonly CheckBox checkBoxBots = new CheckBox();
        private readonly CheckBox checkBoxHltv = new CheckBox();
        private readonly CheckBox checkBoxWait = new CheckBox();
        private readonly LockableCheckBox checkBoxWarn = new LockableCheckBox();
        private readonly LockableCheckBox checkBoxSkip = new LockableCheckBox();

        private class LockableCheckBox : CheckBox
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                if (AutoCheck) { base.OnPaint(e); return; }
                e.Graphics.Clear(BackColor);
                System.Windows.Forms.VisualStyles.CheckBoxState state = Checked ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedDisabled : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedDisabled;
                Size glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
                int top = (ClientSize.Height - glyph.Height) / 2;
                if (Theme.Dark) { CheckBoxRenderer.DrawCheckBox(e.Graphics, new Point(0, top), state); }
                else
                {
                    using (Pen pen = new Pen(Color.FromArgb(96, 96, 96)))
                    {
                        e.Graphics.DrawRectangle(pen, 0, top, glyph.Width - 1, glyph.Height - 1);
                        if (Checked)
                        {
                            float u = glyph.Width / 13f;
                            pen.Width = 1.6f * u;
                            System.Drawing.Drawing2D.SmoothingMode mode = e.Graphics.SmoothingMode;
                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            e.Graphics.DrawLines(pen, new[] { new PointF(3 * u, top + 6.5f * u), new PointF(5.5f * u, top + 9 * u), new PointF(10 * u, top + 3.5f * u) });
                            e.Graphics.SmoothingMode = mode;
                        }
                    }
                }
                Rectangle text = new Rectangle(glyph.Width + 3, 0, ClientSize.Width - glyph.Width - 3, ClientSize.Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, text, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
        private static Color unusable_fore { get { return Theme.Dark ? Color.FromArgb(150, 150, 150) : SystemColors.GrayText; } }
        private Font strike_font;

        private void refresh_scheduling()
        {
            set_usable(checkBoxWarn, !checkBoxWait.Checked);
            set_usable(checkBoxSkip, checkBoxWait.Checked);
        }

        private void set_usable(CheckBox box, bool usable)
        {
            box.AutoCheck = usable;
            box.Invalidate();
            box.ForeColor = usable ? checkBoxWait.ForeColor : unusable_fore;
            if (strike_font == null) { strike_font = new Font(checkBoxWait.Font, FontStyle.Strikeout); }
            box.Font = usable ? checkBoxWait.Font : strike_font;
        }

        private void build_scheduling_tab()
        {
            checkBoxBots.Text = Lang.T("opt.exclude_bots");
            checkBoxBots.AutoSize = true;
            checkBoxHltv.Text = Lang.T("opt.exclude_hltv");
            checkBoxHltv.AutoSize = true;
            tabAppearance.Controls.AddRange(new Control[] { checkBoxBots, checkBoxHltv });

            TabPage page = new TabPage(Lang.T("opt.tab_scheduling"));
            checkBoxWait.Text = Lang.T("opt.wait_empty");
            checkBoxWait.AutoSize = true;
            checkBoxWait.Location = new Point(10, 36);
            checkBoxWarn.Text = Lang.T("opt.warn_rcon");
            checkBoxWarn.AutoSize = true;
            checkBoxWarn.Location = new Point(10, 12);
            checkBoxSkip.Text = Lang.T("opt.skip_busy");
            checkBoxSkip.AutoSize = true;
            checkBoxSkip.Location = new Point(28, 58);
            checkBoxWait.CheckedChanged += (s, e) => refresh_scheduling();
            Label hint = new Label { Text = Lang.T("opt.scheduling_hint"), Location = new Point(8, 90), Size = new Size(298, 130) };
            page.Controls.AddRange(new Control[] { checkBoxWarn, checkBoxWait, checkBoxSkip, hint });
            List<TabPage> pages = tabs.TabPages.Cast<TabPage>().ToList();
            pages.Insert(2, page);
            tabs.TabPages.Clear();
            tabs.TabPages.AddRange(pages.ToArray());
        }

        private static string hook_label(WebhookSetting hook)
        {
            if (hook.Name.Length > 0) { return hook.Name; }
            Uri uri = Core.webhook_uri(hook.Url);
            return uri != null ? uri.Host : hook.Url;
        }

        private static string hook_events(WebhookSetting hook)
        {
            if (!hook.NotifyStart && !hook.NotifyRestart && !hook.NotifyCrashLoop && !hook.NotifySchedule) { return Lang.T("event.none"); }
            if (hook.NotifyStart && hook.NotifyRestart && hook.NotifyCrashLoop && hook.NotifySchedule) { return Lang.T("event.all"); }
            return string.Join(" ", new[] {
                hook.NotifyStart ? Lang.T("event.start") : "-",
                hook.NotifyRestart ? Lang.T("event.restart") : "-",
                hook.NotifyCrashLoop ? Lang.T("event.crash_loop") : "-",
                hook.NotifySchedule ? Lang.T("event.schedule") : "-" });
        }

        private void refresh_hooks(int select)
        {
            listViewHooks.BeginUpdate();
            listViewHooks.Items.Clear();
            foreach (WebhookSetting hook in hooks)
            {
                ListViewItem item = new ListViewItem(hook_label(hook));
                item.SubItems.Add(Core.webhook_kind_name(hook.Url, hook.Type));
                item.SubItems.Add(hook_events(hook));
                item.ToolTipText = hook.Url;
                listViewHooks.Items.Add(item);
            }
            listViewHooks.Columns[2].Width = -2;
            listViewHooks.EndUpdate();
            if (select >= 0 && select < listViewHooks.Items.Count) { listViewHooks.Items[select].Selected = true; }
            refresh_hook_buttons();
        }

        private void refresh_hook_buttons()
        {
            bool selected = listViewHooks.SelectedIndices.Count > 0;
            buttonHookEdit.Enabled = selected;
            buttonHookRemove.Enabled = selected;
            buttonTest.Enabled = selected;
        }

        private int selected_hook { get { return listViewHooks.SelectedIndices.Count > 0 ? listViewHooks.SelectedIndices[0] : -1; } }

        private void listViewHooks_SelectedIndexChanged(object sender, EventArgs e)
        {
            refresh_hook_buttons();
        }

        private void buttonHookAdd_Click(object sender, EventArgs e)
        {
            using (WebhookForm form = new WebhookForm(null))
            {
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                hooks.Add(form.Result);
                refresh_hooks(hooks.Count - 1);
            }
        }

        private void buttonHookEdit_Click(object sender, EventArgs e)
        {
            int index = selected_hook;
            if (index < 0) { return; }
            using (WebhookForm form = new WebhookForm(hooks[index]))
            {
                if (form.ShowDialog(this) != DialogResult.OK) { return; }
                hooks[index] = form.Result;
                refresh_hooks(index);
            }
        }

        private void buttonHookRemove_Click(object sender, EventArgs e)
        {
            int index = selected_hook;
            if (index < 0) { return; }
            hooks.RemoveAt(index);
            refresh_hooks(Math.Min(index, hooks.Count - 1));
        }

        private void buttonImport_Click(object sender, EventArgs e)
        {
            if (import_configs == null) { return; }
            import_configs();
            numInterval.Value = Core.Instance.opt_check_interval;
            numProcessRetries.Value = Core.Instance.opt_process_retries;
            numNetworkRetries.Value = Core.Instance.opt_network_retries;
            numMaxRestarts.Value = Core.Instance.opt_max_restarts;
            checkTray.Checked = Core.Instance.opt_tray;
        }

        private void OptionsForm_Shown(object sender, EventArgs e)
        {
            numInterval.Value = Core.Instance.opt_check_interval;
            numProcessRetries.Value = Core.Instance.opt_process_retries;
            numNetworkRetries.Value = Core.Instance.opt_network_retries;
            numMaxRestarts.Value = Core.Instance.opt_max_restarts;
            numCrashLimit.Value = Core.Instance.opt_loop_count;
            numShutdown.Value = Core.Instance.opt_shutdown;
            comboBoxData.SelectedIndex = Core.data_in_appdata ? 1 : 0;
            checkBoxBots.Checked = Core.Instance.opt_exclude_bots;
            checkBoxHltv.Checked = Core.Instance.opt_exclude_hltv;
            checkBoxWait.Checked = Core.Instance.opt_wait_empty;
            checkBoxWarn.Checked = Core.Instance.opt_warn_rcon;
            checkBoxSkip.Checked = Core.Instance.opt_skip_busy;
            refresh_scheduling();
            checkTray.Checked = Core.Instance.opt_tray;
            checkBoxLocalIps.Checked = Core.Instance.opt_list_local_ips;
            hooks = Core.Instance.opt_webhooks;
            refresh_hooks(-1);

            foreach (string mode in Theme.Modes) { comboTheme.Items.Add(Lang.T("theme." + mode.ToLowerInvariant())); }
            comboTheme.SelectedIndex = Math.Max(0, Array.IndexOf(Theme.Modes, Core.Instance.opt_theme));

            languages = Lang.available();
            foreach (Lang.Info info in languages) { comboLanguage.Items.Add(info.Name); }
            if (languages.Count == 0) { comboLanguage.Items.Add("English"); comboLanguage.Enabled = false; }
            int lang_index = languages.FindIndex(l => l.Id.Equals(Lang.Current, StringComparison.OrdinalIgnoreCase));
            comboLanguage.SelectedIndex = Math.Max(0, lang_index);

            comboIcon.Items.Add(Lang.T("opt.default_icon"));
            comboIcon.Items.AddRange(Core.Instance.icon_list());
            int index = comboIcon.Items.IndexOf(Core.Instance.opt_icon);
            comboIcon.SelectedIndex = index > 0 ? index : 0;

            refresh_service();
        }

        private void refresh_service()
        {
            string status = ServiceHelper.Status();
            labelServiceStatus.Text = Lang.F("opt.service_status", Lang.service_status(status));
            buttonService.Text = Lang.T(status == "Not installed" ? "opt.service_install" : "opt.service_uninstall");
        }

        private void comboIcon_SelectedIndexChanged(object sender, EventArgs e)
        {
            string name = comboIcon.SelectedIndex > 0 ? comboIcon.Text : "";
            if (iconPreview.Image != null) { iconPreview.Image.Dispose(); }
            iconPreview.Image = Core.Instance.icon_preview(name, 32);
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            int index = selected_hook;
            if (index < 0) { return; }
            string url = hooks[index].Url;
            string type = hooks[index].Type;
            string resolved;
            Cursor = Cursors.WaitCursor;
            string error = Core.Instance.post_webhook(url, type, Lang.T("webhook.test_message"), out resolved);
            Cursor = Cursors.Default;
            if (resolved != null && resolved != url)
            {
                hooks[index].Url = resolved;
                refresh_hooks(index);
            }
            if (error == null) { MessageBox.Show(this, Lang.T("webhook.sent"), Lang.T("webhook.title"), MessageBoxButtons.OK, MessageBoxIcon.Information); }
            else { MessageBox.Show(this, Lang.F("webhook.failed", error), Lang.T("webhook.title"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void buttonService_Click(object sender, EventArgs e)
        {
            if (!ServiceHelper.Installed)
            {
                if (MessageBox.Show(this, Lang.T("svc.install_question"), Lang.T("opt.tab_service"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) { return; }
                if (!save()) { return; }
                ServiceHelper.Install();
                if (ServiceHelper.Installed)
                {
                    service_installed = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                MessageBox.Show(this, Lang.T("svc.install_failed"), Lang.T("opt.tab_service"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                if (MessageBox.Show(this, Lang.T("svc.uninstall_question"), Lang.T("opt.tab_service"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) { return; }
                ServiceHelper.Uninstall();
                if (ServiceHelper.Installed) { MessageBox.Show(this, Lang.T("svc.uninstall_failed"), Lang.T("opt.tab_service"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            refresh_service();
        }

        private bool save()
        {
            Core.Instance.opt_check_interval = (int)numInterval.Value;
            Core.Instance.opt_process_retries = (int)numProcessRetries.Value;
            Core.Instance.opt_network_retries = (int)numNetworkRetries.Value;
            Core.Instance.opt_max_restarts = (int)numMaxRestarts.Value;
            Core.Instance.opt_loop_count = (int)numCrashLimit.Value;
            Core.Instance.opt_shutdown = (int)numShutdown.Value;
            Core.Instance.opt_exclude_bots = checkBoxBots.Checked;
            Core.Instance.opt_exclude_hltv = checkBoxHltv.Checked;
            Core.Instance.opt_wait_empty = checkBoxWait.Checked;
            Core.Instance.opt_warn_rcon = checkBoxWarn.Checked;
            Core.Instance.opt_skip_busy = checkBoxSkip.Checked;
            Core.Instance.opt_tray = checkTray.Checked;
            Core.Instance.opt_list_local_ips = checkBoxLocalIps.Checked;
            Core.Instance.opt_webhooks = hooks;
            Core.Instance.opt_theme = Theme.Modes[Math.Max(0, comboTheme.SelectedIndex)];
            Core.Instance.opt_icon = comboIcon.SelectedIndex > 0 ? comboIcon.Text : "";
            if (languages.Count > 0 && comboLanguage.SelectedIndex >= 0) { Core.Instance.opt_language = languages[comboLanguage.SelectedIndex].Id; }
            try { if (Core.Instance.move_data(comboBoxData.SelectedIndex == 1) && ServiceHelper.Running) { ServiceHelper.Restart(); } }
            catch (Exception ex) { MessageBox.Show(this, Lang.F("log.move_data_failed", ex.Message), Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }

            try { Core.Instance.SaveConfig(); }
            catch (Exception ex) { MessageBox.Show(this, Lang.F("log.save_settings_failed", ex.Message), Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            return true;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!save()) { return; }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            if (!save()) { return; }
            if (applied != null) { applied(); }
            Theme.Apply(this);
            refresh_scheduling();
        }

    }
}
