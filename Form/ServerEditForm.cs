using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace hlsmx
{
    public partial class ServerEditForm : Form
    {
        public ServerEditForm()
        {
            InitializeComponent();
            this.Icon = Core.Instance.app_icon;
            labelName.Text = Lang.T("edit.name");
            labelPriority.Text = Lang.T("edit.priority");
            checkHidden.Text = Lang.T("edit.hide_console");
            labelExe.Text = Lang.T("edit.exe");
            buttonBrowse.Text = Lang.T("edit.browse");
            labelParams.Text = Lang.T("edit.params");
            buttonOk.Text = Lang.T("dlg.ok");
            buttonAffinity.Text = Lang.T("edit.affinity_show");
            groupAffinity.Text = Lang.T("edit.affinity");
            labelRcon.Text = Lang.T("edit.rcon");
            labelRconHint.Text = Lang.T("edit.rcon_hint");
            comboPriority.Items.Clear();
            foreach (string p in Priorities) { comboPriority.Items.Add(Lang.priority(p)); }
            labelTemplate.Text = Lang.T("edit.template");
            comboTemplate.Items.Clear();
            foreach (string[] t in Templates) { comboTemplate.Items.Add(t[0]); }
            Theme.Apply(this);
            fit_layout();
        }

        private int text_width(Control c, string text, int padding)
        {
            return TextRenderer.MeasureText(text, c.Font).Width + padding;
        }

        private void fit_layout()
        {
            int field = Math.Max(83, Math.Max(labelName.PreferredWidth, Math.Max(labelExe.PreferredWidth, labelRcon.PreferredWidth)) + labelName.Left + 8);
            textName.Left = field;
            labelPriority.Left = textName.Right + 9;
            int combo = 74;
            foreach (object item in comboPriority.Items) { combo = Math.Max(combo, text_width(comboPriority, item.ToString(), 28)); }
            comboPriority.Left = labelPriority.Left + labelPriority.PreferredWidth + 4;
            comboPriority.Width = combo;
            checkHidden.Left = comboPriority.Right + 10;
            textRcon.Left = field;
            labelRconHint.Left = textRcon.Right + 6;

            buttonBrowse.Width = Math.Max(75, text_width(buttonBrowse, buttonBrowse.Text, 20));
            buttonOk.Width = Math.Max(75, text_width(buttonOk, buttonOk.Text, 20));
            buttonAffinity.Width = Math.Max(111, Math.Max(text_width(buttonAffinity, Lang.T("edit.affinity_show"), 20), text_width(buttonAffinity, Lang.T("edit.affinity_hide"), 20)));

            int width = 444;
            width = Math.Max(width, checkHidden.Right + 8);
            width = Math.Max(width, labelRconHint.Left + labelRconHint.PreferredWidth + 8);
            int template = 74;
            foreach (object item in comboTemplate.Items) { template = Math.Max(template, text_width(comboTemplate, item.ToString(), 28)); }
            comboTemplate.Width = template;
            width = Math.Max(width, labelParams.Left + labelParams.PreferredWidth + 12 + labelTemplate.PreferredWidth + 4 + template + 5);
            width = Math.Max(width, field + 200 + 6 + buttonBrowse.Width + 5);
            ClientSize = new Size(width, ClientSize.Height);

            buttonBrowse.Left = width - 5 - buttonBrowse.Width;
            textExe.Left = field;
            textExe.Width = buttonBrowse.Left - 6 - field;
            textParams.Width = width - 12;
            comboTemplate.Left = width - 5 - comboTemplate.Width;
            labelTemplate.Left = comboTemplate.Left - 4 - labelTemplate.PreferredWidth;
            buttonAffinity.Left = width - 5 - buttonAffinity.Width;
            buttonOk.Left = buttonAffinity.Left - 6 - buttonOk.Width;
            groupAffinity.Width = width - 13;
            listCores.Width = groupAffinity.Width - 6;
        }
        public static readonly string[] Priorities = { "Low", "Below Normal", "Normal", "Above Normal", "High", "Realtime" };
        public static readonly string[] PortArgs = { "-port", "+port", "+hostport" };
        public static readonly string[][] Templates =
        {
            new[] { "Half-Life", "-console -game valve -port 27015 +maxplayers 16 +map crossfire" },
            new[] { "Counter-Strike 1.6", "-console -game cstrike -port 27015 +maxplayers 32 +map de_dust2" },
            new[] { "Counter-Strike: Source", "-console -game cstrike -port 27015 +maxplayers 32 +map de_dust2" },
            new[] { "Counter-Strike 2", "-dedicated -console -usercon -port 27015 +game_type 0 +game_mode 1 +map de_dust2" },
            new[] { "Team Fortress 2", "-console -game tf -port 27015 +maxplayers 24 +map ctf_2fort" },
            new[] { "Garry's Mod", "-console -game garrysmod -port 27015 +maxplayers 16 +gamemode sandbox +map gm_construct" },
            new[] { "Left 4 Dead", "-console -game left4dead -port 27015 +map l4d_hospital01_apartment" },
            new[] { "Left 4 Dead 2", "-console -game left4dead2 -port 27015 +map c1m1_hotel" },
        };
        private static string[] split_args(string launch)
        {
            return (launch ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        public static bool has_port(string launch)
        {
            string[] args = split_args(launch);
            return args.Take(args.Length - 1).Any(a => PortArgs.Contains(a));
        }
        public static bool has_arg(string launch, string arg)
        {
            return split_args(launch).Any(a => a.Equals(arg, StringComparison.OrdinalIgnoreCase));
        }
        public static bool is_cs2(string exe)
        {
            try { return string.Equals(System.IO.Path.GetFileName((exe ?? "").Trim()), "cs2.exe", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        private bool affinity_open;
        private string initial_priority = "";
        private string initial_cores = "";
        public bool AllCoresByDefault;
        public string ServerName { get { return textName.Text; } set { textName.Text = value; } }
        public string Priority
        {
            get { return comboPriority.SelectedIndex < 0 ? "Normal" : Priorities[comboPriority.SelectedIndex]; }
            set { initial_priority = value ?? ""; }
        }
        public bool HideConsole { get { return checkHidden.Checked; } set { checkHidden.Checked = value; } }
        public string ExePath { get { return textExe.Text; } set { textExe.Text = value; } }
        public string LaunchParams
        {
            get { return Regex.Replace(textParams.Text, "[\r\n]+", " ").Trim(); }
            set { textParams.Text = value; }
        }
        public string Cores
        {
            get { return string.Concat(listCores.CheckedIndices.Cast<int>().Select(i => i.ToString() + "|")); }
            set { initial_cores = value ?? ""; }
        }
        public string RconPassword { get { return textRcon.Text; } set { textRcon.Text = value; } }

        private void ServerEditForm_Shown(object sender, EventArgs e)
        {
            int priority = Array.IndexOf(Priorities, initial_priority);
            comboPriority.SelectedIndex = priority >= 0 ? priority : Array.IndexOf(Priorities, "Normal");
            ClientSize = new Size(ClientSize.Width, buttonOk.Bottom + 8);
            groupAffinity.Visible = false;

            int count = Environment.ProcessorCount;
            listCores.Items.AddRange(Enumerable.Range(0, count).Select(i => (object)("CPU " + i)).ToArray());
            List<int> saved = new List<int>();
            bool invalid = false;
            foreach (string part in initial_cores.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int core;
                if (int.TryParse(part, out core) && core >= 0 && core < count) { saved.Add(core); } else { invalid = true; }
            }
            for (int i = 0; i < count; i++) { listCores.SetItemChecked(i, saved.Count > 0 ? saved.Contains(i) : AllCoresByDefault); }
            if (invalid)
            {
                MessageBox.Show(this, Lang.T("edit.bad_core"), Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                buttonAffinity_Click(null, null);
            }
        }

        private void buttonAffinity_Click(object sender, EventArgs e)
        {
            affinity_open = !affinity_open;
            this.ClientSize = new Size(this.ClientSize.Width, (affinity_open ? groupAffinity.Bottom : buttonOk.Bottom) + 8);
            groupAffinity.Visible = affinity_open;
            buttonAffinity.Text = Lang.T(affinity_open ? "edit.affinity_hide" : "edit.affinity_show");
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = Lang.T("edit.filter");
                if (dialog.ShowDialog(this) == DialogResult.OK) { textExe.Text = dialog.FileName; }
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            string problem = null;
            if (new[] { textName, textExe, textParams }.Any(t => string.IsNullOrWhiteSpace(t.Text))) { problem = "edit.empty"; }
            else if (!has_port(LaunchParams)) { problem = "edit.no_port"; }
            else if (listCores.CheckedIndices.Count == 0) { problem = "edit.no_cores"; }
            if (problem != null)
            {
                MessageBox.Show(this, Lang.T(problem), Lang.T("dlg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (problem == "edit.no_cores" && !affinity_open) { buttonAffinity_Click(null, null); }
                return;
            }
            if (is_cs2(ExePath))
            {
                string warning = null;
                if (!has_arg(LaunchParams, "-dedicated")) { warning = "edit.cs2_dedicated"; }
                else if (!has_arg(LaunchParams, "-usercon") && (RconPassword.Length > 0 || Rcon.password_from_params(LaunchParams).Length > 0)) { warning = "edit.cs2_usercon"; }
                if (warning != null && MessageBox.Show(this, Lang.T(warning), Lang.T("dlg.warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { return; }
            }
            DialogResult = DialogResult.OK;
        }

        private void comboTemplate_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (comboTemplate.SelectedIndex < 0) { return; }
            textParams.Text = Templates[comboTemplate.SelectedIndex][1];
        }
    }
}
