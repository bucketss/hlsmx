using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace hlsmx
{
    public partial class ScheduleForm : Form
    {
        public ScheduleForm()
        {
            InitializeComponent();
            this.Icon = Core.Instance.app_icon;
            Text = Lang.T("sched.title");
            checkBox1.Text = Lang.T("sched.enabled");
            buttonAdd.Text = Lang.T("sched.add");
            buttonUpdate.Text = Lang.T("sched.update");
            buttonRemove.Text = Lang.T("sched.remove");
            buttonOK.Text = Lang.T("dlg.ok");
            buttonCancel.Text = Lang.T("dlg.cancel");
            Theme.Apply(this);
            foreach (string d in ScheduleEntry.Days) { comboBox1.Items.Add(day_text(d)); }
            foreach (string a in ScheduleEntry.Actions) { comboBox2.Items.Add(action_text(a)); }
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            dateTimePicker1.Value = DateTime.Today.AddHours(5);
        }

        private List<ScheduleEntry> schedules = new List<ScheduleEntry>();
        public List<ScheduleEntry> Schedules
        {
            get { return schedules.Select(s => s.copy()).ToList(); }
            set
            {
                schedules = value.Select(s => s.copy()).ToList();
                refresh_list();
            }
        }

        private static string day_text(string day) { return Lang.T("day." + day.ToLowerInvariant()); }
        private static string action_text(string action) { return Lang.T("action." + action.ToLowerInvariant()); }
        private static string display(ScheduleEntry s)
        {
            return (s.Enabled ? "" : Lang.T("sched.off") + " ") + day_text(s.Day) + " " + s.Time + ": " + action_text(s.Action);
        }

        private void refresh_list()
        {
            int selected = listBox1.SelectedIndex;
            listBox1.Items.Clear();
            foreach (ScheduleEntry s in schedules) { listBox1.Items.Add(display(s)); }
            if (selected >= 0 && selected < listBox1.Items.Count) { listBox1.SelectedIndex = selected; }
        }

        private ScheduleEntry from_controls()
        {
            ScheduleEntry s = new ScheduleEntry();
            s.Enabled = checkBox1.Checked;
            s.Day = ScheduleEntry.Days[Math.Max(0, comboBox1.SelectedIndex)];
            s.Time = dateTimePicker1.Value.ToString("HH:mm");
            s.Action = ScheduleEntry.Actions[Math.Max(0, comboBox2.SelectedIndex)];
            return s;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) { return; }
            ScheduleEntry s = schedules[listBox1.SelectedIndex];
            checkBox1.Checked = s.Enabled;
            comboBox1.SelectedIndex = Math.Max(0, Array.IndexOf(ScheduleEntry.Days, s.Day));
            comboBox2.SelectedIndex = Math.Max(0, Array.IndexOf(ScheduleEntry.Actions, s.Action));
            TimeSpan time;
            if (TimeSpan.TryParse(s.Time, out time)) { dateTimePicker1.Value = DateTime.Today + time; }
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            schedules.Add(from_controls());
            refresh_list();
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) { return; }
            schedules[listBox1.SelectedIndex] = from_controls();
            refresh_list();
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) { return; }
            schedules.RemoveAt(listBox1.SelectedIndex);
            refresh_list();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
