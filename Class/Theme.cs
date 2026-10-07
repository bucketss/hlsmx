using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace hlsmx
{
    static class Theme
    {
        public static readonly string[] Modes = { "System", "Light", "Dark" };

        public static readonly Color Back = Color.FromArgb(32, 32, 32);
        public static readonly Color Surface = Color.FromArgb(43, 43, 43);
        public static readonly Color Fore = Color.FromArgb(230, 230, 230);
        public static readonly Color Border = Color.FromArgb(70, 70, 70);
        public static readonly Color Highlight = Color.FromArgb(0, 90, 158);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string app, string idList);
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        public static bool Dark
        {
            get
            {
                string mode = Core.Instance.opt_theme;
                if (mode == "Dark") { return true; }
                if (mode == "Light") { return false; }
                try
                {
                    object value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                    return value is int && (int)value == 0;
                }
                catch { return false; }
            }
        }

        public static void Apply(Form form)
        {
            bool dark = Dark;
            ToolStripManager.Renderer = dark ? (ToolStripRenderer)new DarkRenderer() : new ToolStripProfessionalRenderer();
            form.BackColor = dark ? Back : SystemColors.Control;
            form.ForeColor = dark ? Fore : SystemColors.ControlText;
            if (form.IsHandleCreated) { apply_title_bar(form, dark); }
            else { form.HandleCreated += (s, e) => apply_title_bar(form, Dark); }
            apply_children(form, dark);
        }

        public static void ApplyTo(Control c)
        {
            bool dark = Dark;
            apply_control(c, dark);
            apply_children(c, dark);
        }

        private static void apply_title_bar(Form form, bool dark)
        {
            int value = dark ? 1 : 0;
            if (DwmSetWindowAttribute(form.Handle, 20, ref value, 4) != 0) { DwmSetWindowAttribute(form.Handle, 19, ref value, 4); }
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020);
        }

        private static void apply_children(Control parent, bool dark)
        {
            foreach (Control c in parent.Controls)
            {
                apply_control(c, dark);
                apply_children(c, dark);
            }
        }

        private static void apply_control(Control c, bool dark)
        {
            if (c is TextBox || c is ListBox || c is NumericUpDown || c is ComboBox || c is DateTimePicker)
            {
                c.BackColor = dark ? Surface : SystemColors.Window;
                c.ForeColor = dark ? Fore : SystemColors.WindowText;
                ComboBox combo = c as ComboBox;
                if (combo != null) { combo.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard; }
                if (c is TextBox && ((TextBox)c).Multiline) { set_theme(c, dark ? "DarkMode_Explorer" : null); }
            }
            else if (c is ListView)
            {
                ListView lv = (ListView)c;
                lv.BackColor = dark ? Surface : SystemColors.Window;
                lv.ForeColor = dark ? Fore : SystemColors.WindowText;
                if (lv.IsHandleCreated) { theme_list_view(lv, dark); }
                else { lv.HandleCreated += (s, e) => theme_list_view(lv, Dark); }
            }
            else if (c is Button)
            {
                Button b = (Button)c;
                b.FlatStyle = dark ? FlatStyle.Flat : FlatStyle.Standard;
                b.FlatAppearance.BorderColor = Border;
                b.BackColor = dark ? Surface : SystemColors.Control;
                b.ForeColor = dark ? Fore : SystemColors.ControlText;
                b.UseVisualStyleBackColor = !dark;
            }
            else if (c is TabPage)
            {
                TabPage page = (TabPage)c;
                page.BackColor = dark ? Back : SystemColors.Window;
                page.ForeColor = dark ? Fore : SystemColors.ControlText;
                page.UseVisualStyleBackColor = !dark;
            }
            else if (c is ThemedTabControl)
            {
                ((ThemedTabControl)c).Dark = dark;
            }
            else if (c is ToolStrip)
            {
                c.BackColor = dark ? Back : SystemColors.Control;
                c.ForeColor = dark ? Fore : SystemColors.ControlText;
            }
            else
            {
                c.BackColor = dark ? Back : (c.Parent != null ? c.Parent.BackColor : SystemColors.Control);
                c.ForeColor = dark ? Fore : SystemColors.ControlText;
                CheckBox check = c as CheckBox;
                if (check != null) { check.UseVisualStyleBackColor = !dark; }
            }
        }

        private static void theme_list_view(ListView lv, bool dark)
        {
            set_theme(lv, dark ? "DarkMode_Explorer" : null);
            IntPtr header = SendMessage(lv.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
            if (header != IntPtr.Zero) { SetWindowTheme(header, dark ? "DarkMode_ItemsView" : null, null); }
            HeaderText hook;
            if (!header_hooks.TryGetValue(lv, out hook) || hook.Handle != lv.Handle)
            {
                hook = new HeaderText();
                hook.AssignHandle(lv.Handle);
                header_hooks[lv] = hook;
            }
            hook.Header = header;
            hook.Dark = dark;
            if (header != IntPtr.Zero) { InvalidateRect(header, IntPtr.Zero, true); }
        }

        static readonly System.Collections.Generic.Dictionary<ListView, HeaderText> header_hooks = new System.Collections.Generic.Dictionary<ListView, HeaderText>();

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hwnd, IntPtr rect, bool erase);
        [DllImport("gdi32.dll")]
        static extern int SetTextColor(IntPtr hdc, int color);

        [StructLayout(LayoutKind.Sequential)]
        struct NMHDR { public IntPtr hwndFrom; public UIntPtr idFrom; public int code; }
        [StructLayout(LayoutKind.Sequential)]
        struct NMCUSTOMDRAW { public NMHDR hdr; public int dwDrawStage; public IntPtr hdc; }

        class HeaderText : NativeWindow
        {
            public IntPtr Header;
            public bool Dark;
            protected override void WndProc(ref Message m)
            {
                if (Dark && m.Msg == 0x004E)
                {
                    NMHDR hdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));
                    if (hdr.code == -12 && hdr.hwndFrom == Header)
                    {
                        NMCUSTOMDRAW cd = (NMCUSTOMDRAW)Marshal.PtrToStructure(m.LParam, typeof(NMCUSTOMDRAW));
                        if (cd.dwDrawStage == 0x1) { m.Result = (IntPtr)0x20; return; }
                        if (cd.dwDrawStage == 0x10001)
                        {
                            SetTextColor(cd.hdc, ColorTranslator.ToWin32(Fore));
                            m.Result = IntPtr.Zero;
                            return;
                        }
                    }
                }
                base.WndProc(ref m);
            }
        }

        private static void set_theme(Control c, string name)
        {
            if (c.IsHandleCreated) { SetWindowTheme(c.Handle, name, null); }
            else { c.HandleCreated += (s, e) => SetWindowTheme(c.Handle, name, null); }
        }

        public static Rectangle header_bounds(ListView lv)
        {
            IntPtr header = SendMessage(lv.Handle, 0x101F, IntPtr.Zero, IntPtr.Zero);
            RECT r;
            if (header == IntPtr.Zero || !GetWindowRect(header, out r)) { return Rectangle.Empty; }
            return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        class DarkColors : ProfessionalColorTable
        {
            public override Color MenuStripGradientBegin { get { return Back; } }
            public override Color MenuStripGradientEnd { get { return Back; } }
            public override Color ToolStripDropDownBackground { get { return Surface; } }
            public override Color ImageMarginGradientBegin { get { return Surface; } }
            public override Color ImageMarginGradientMiddle { get { return Surface; } }
            public override Color ImageMarginGradientEnd { get { return Surface; } }
            public override Color MenuItemSelected { get { return Highlight; } }
            public override Color MenuItemSelectedGradientBegin { get { return Highlight; } }
            public override Color MenuItemSelectedGradientEnd { get { return Highlight; } }
            public override Color MenuItemPressedGradientBegin { get { return Surface; } }
            public override Color MenuItemPressedGradientEnd { get { return Surface; } }
            public override Color MenuItemBorder { get { return Highlight; } }
            public override Color MenuBorder { get { return Border; } }
            public override Color SeparatorDark { get { return Border; } }
            public override Color SeparatorLight { get { return Border; } }
            public override Color CheckBackground { get { return Highlight; } }
            public override Color CheckSelectedBackground { get { return Highlight; } }
            public override Color CheckPressedBackground { get { return Highlight; } }
        }

        class DarkRenderer : ToolStripProfessionalRenderer
        {
            public DarkRenderer() : base(new DarkColors()) { RoundedEdges = false; }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = e.Item.Enabled ? Fore : Color.FromArgb(120, 120, 120);
                base.OnRenderItemText(e);
            }
            protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
            {
                e.ArrowColor = Fore;
                base.OnRenderArrow(e);
            }
            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                Rectangle r = new Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2, e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
                using (SolidBrush b = new SolidBrush(Highlight)) { e.Graphics.FillRectangle(b, r); }
                TextRenderer.DrawText(e.Graphics, "✓", e.Item.Font, r, Fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    class ThemedTabControl : TabControl
    {
        public ThemedTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        private bool dark;
        public bool Dark
        {
            get { return dark; }
            set
            {
                if (dark == value) { return; }
                dark = value;
                Invalidate();
            }
        }

        public Func<TabPage, Color> TabColor;
        public Color BarColor;
        public event EventHandler Reordered;
        public bool Moving { get; private set; }
        private TabPage drag_page;
        private Point drag_origin;
        private bool dragging;

        public int TabAt(Point p)
        {
            for (int i = 0; i < TabCount; i++) { if (GetTabRect(i).Contains(p)) { return i; } }
            return -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = TabAt(e.Location);
            drag_page = e.Button == MouseButtons.Left && index >= 0 ? TabPages[index] : null;
            drag_origin = e.Location;
            dragging = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (drag_page == null || e.Button != MouseButtons.Left) { return; }
            if (!dragging && Math.Abs(e.X - drag_origin.X) < SystemInformation.DragSize.Width) { return; }
            dragging = true;
            int target = TabAt(e.Location);
            int from = TabPages.IndexOf(drag_page);
            if (target < 0 || from < 0 || target == from) { return; }
            Rectangle over = GetTabRect(target);
            int width = GetTabRect(from).Width;
            if (target > from ? e.X < over.Right - width : e.X > over.Left + width) { return; }
            Moving = true;
            try
            {
                SuspendLayout();
                TabPages.Remove(drag_page);
                TabPages.Insert(target, drag_page);
                SelectedTab = drag_page;
                ResumeLayout();
            }
            finally { Moving = false; }
            if (Reordered != null) { Reordered(this, EventArgs.Empty); }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            drag_page = null;
            dragging = false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr handle);
        private IntPtr hfont = IntPtr.Zero;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (hfont != IntPtr.Zero) { DeleteObject(hfont); }
            hfont = Font.ToHfont();
            SendMessage(Handle, 0x0030, hfont, (IntPtr)1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (hfont != IntPtr.Zero) { DeleteObject(hfont); hfont = IntPtr.Zero; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = dark ? Theme.Back : SystemColors.Control;
            Color surface = dark ? Theme.Surface : SystemColors.Window;
            Color edge = dark ? Theme.Border : SystemColors.ControlDark;
            Color fore = dark ? Theme.Fore : SystemColors.ControlText;
            using (SolidBrush b = new SolidBrush(back)) { g.FillRectangle(b, ClientRectangle); }
            Rectangle display = DisplayRectangle;
            display.Inflate(1, 1);
            using (Pen border = new Pen(edge)) { g.DrawRectangle(border, display); }
            for (int i = 0; i < TabCount; i++)
            {
                Rectangle tab = GetTabRect(i);
                bool selected = i == SelectedIndex;
                Color custom = TabColor != null ? TabColor(TabPages[i]) : Color.Empty;
                Color fill = !custom.IsEmpty ? custom : (selected ? surface : back);
                Color text = custom.IsEmpty ? fore : (custom.R * 299 + custom.G * 587 + custom.B * 114 < 128000 ? Color.White : Color.Black);
                using (SolidBrush b = new SolidBrush(fill)) { g.FillRectangle(b, tab); }
                using (Pen border = new Pen(edge)) { g.DrawRectangle(border, tab); }
                if (selected) { using (SolidBrush b = new SolidBrush(BarColor.IsEmpty ? Theme.Highlight : BarColor)) { g.FillRectangle(b, tab.X + 1, tab.Y + 1, tab.Width - 1, 2); } }
                TextRenderer.DrawText(g, TabPages[i].Text, Font, tab, text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }
}
