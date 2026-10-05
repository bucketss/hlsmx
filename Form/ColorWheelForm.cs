using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace hlsmx
{
    class ColorWheelForm : Form
    {
        private readonly Wheel wheel = new Wheel();
        private readonly TrackBar brightness = new TrackBar();
        private readonly Panel preview = new Panel();
        private readonly TextBox hex = new TextBox();
        private bool updating;

        public Color Selected { get; private set; }

        public ColorWheelForm(Color initial)
        {
            Text = Lang.T("wheel.title");
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(236, 346);
            Icon = Core.Instance.app_icon;

            wheel.Location = new Point(18, 12);
            wheel.Size = new Size(200, 200);
            wheel.Changed += (s, e) => update_from_wheel();

            brightness.Location = new Point(12, 218);
            brightness.Size = new Size(212, 30);
            brightness.Minimum = 0;
            brightness.Maximum = 100;
            brightness.TickStyle = TickStyle.None;
            brightness.ValueChanged += (s, e) => { wheel.Value = brightness.Value / 100f; update_from_wheel(); };

            preview.Location = new Point(12, 268);
            preview.Size = new Size(60, 23);
            preview.BorderStyle = BorderStyle.FixedSingle;

            hex.Location = new Point(80, 269);
            hex.Size = new Size(144, 23);
            hex.TextChanged += (s, e) => update_from_hex();

            Button ok = new Button { Text = Lang.T("dlg.ok"), DialogResult = DialogResult.OK, Location = new Point(68, 308), Size = new Size(75, 26) };
            Button cancel = new Button { Text = Lang.T("dlg.cancel"), DialogResult = DialogResult.Cancel, Location = new Point(149, 308), Size = new Size(75, 26) };
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.AddRange(new Control[] { wheel, brightness, preview, hex, ok, cancel });
            Theme.Apply(this);
            set_color(initial.IsEmpty ? Color.FromArgb(200, 220, 248) : initial);
        }

        private void set_color(Color c)
        {
            float h, s, v;
            to_hsv(c, out h, out s, out v);
            updating = true;
            wheel.Hue = h;
            wheel.Saturation = s;
            wheel.Value = v;
            brightness.Value = (int)Math.Round(v * 100);
            updating = false;
            show_color(c, true);
        }

        private void update_from_wheel()
        {
            if (updating) { return; }
            show_color(from_hsv(wheel.Hue, wheel.Saturation, wheel.Value), true);
        }

        private void update_from_hex()
        {
            if (updating) { return; }
            Color c;
            if (!try_parse(hex.Text, out c)) { return; }
            float h, s, v;
            to_hsv(c, out h, out s, out v);
            updating = true;
            wheel.Hue = h;
            wheel.Saturation = s;
            wheel.Value = v;
            brightness.Value = (int)Math.Round(v * 100);
            updating = false;
            show_color(c, false);
        }

        private void show_color(Color c, bool write_hex)
        {
            Selected = c;
            preview.BackColor = c;
            if (write_hex)
            {
                updating = true;
                hex.Text = to_hex(c);
                updating = false;
            }
        }

        public static string to_hex(Color c) { return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B); }

        public static bool try_parse(string text, out Color c)
        {
            c = Color.Empty;
            if (string.IsNullOrEmpty(text)) { return false; }
            string t = text.Trim().TrimStart('#');
            int rgb;
            if (t.Length != 6 || !int.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out rgb)) { return false; }
            c = Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            return true;
        }

        private static void to_hsv(Color c, out float h, out float s, out float v)
        {
            float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
            float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            float d = max - min;
            v = max;
            s = max == 0 ? 0 : d / max;
            if (d == 0) { h = 0; }
            else if (max == r) { h = 60 * (((g - b) / d) % 6); }
            else if (max == g) { h = 60 * ((b - r) / d + 2); }
            else { h = 60 * ((r - g) / d + 4); }
            if (h < 0) { h += 360; }
        }

        public static Color from_hsv(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r, g, b;
            if (h < 60) { r = c; g = x; b = 0; }
            else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; }
            else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; }
            else { r = c; g = 0; b = x; }
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }

        class Wheel : Control
        {
            public event EventHandler Changed;
            private Bitmap image;
            private float value = 1;
            public float Hue;
            public float Saturation;
            public float Value
            {
                get { return value; }
                set { if (this.value != value) { this.value = value; render(); } Invalidate(); }
            }

            public Wheel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Cross;
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                render();
            }

            private void render()
            {
                if (Width < 1 || Height < 1) { return; }
                if (image != null) { image.Dispose(); }
                int size = Math.Min(Width, Height);
                image = new Bitmap(size, size, PixelFormat.Format32bppArgb);
                int[] pixels = new int[size * size];
                float r = size / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - r, dy = y + 0.5f - r;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist > r) { continue; }
                        float h = (float)(Math.Atan2(-dy, dx) * 180 / Math.PI);
                        if (h < 0) { h += 360; }
                        Color c = from_hsv(h % 360, Math.Min(1, dist / r), value);
                        int alpha = dist > r - 1 ? (int)(255 * (r - dist)) : 255;
                        pixels[y * size + x] = (alpha << 24) | (c.R << 16) | (c.G << 8) | c.B;
                    }
                }
                BitmapData data = image.LockBits(new Rectangle(0, 0, size, size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
                image.UnlockBits(data);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(Parent != null ? Parent.BackColor : BackColor);
                if (image == null) { return; }
                e.Graphics.DrawImage(image, 0, 0);
                float r = image.Width / 2f;
                double angle = Hue * Math.PI / 180;
                float px = r + (float)(Math.Cos(angle) * Saturation * r);
                float py = r - (float)(Math.Sin(angle) * Saturation * r);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen outer = new Pen(Color.Black, 3)) { e.Graphics.DrawEllipse(outer, px - 5, py - 5, 10, 10); }
                using (Pen inner = new Pen(Color.White, 1.5f)) { e.Graphics.DrawEllipse(inner, px - 5, py - 5, 10, 10); }
            }

            protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); pick(e.Location); }
            protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (e.Button == MouseButtons.Left) { pick(e.Location); } }

            private void pick(Point p)
            {
                if (image == null) { return; }
                float r = image.Width / 2f;
                float dx = p.X - r, dy = p.Y - r;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float h = (float)(Math.Atan2(-dy, dx) * 180 / Math.PI);
                Hue = h < 0 ? h + 360 : h;
                Saturation = Math.Min(1, dist / r);
                Invalidate();
                if (Changed != null) { Changed(this, EventArgs.Empty); }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing && image != null) { image.Dispose(); }
                base.Dispose(disposing);
            }
        }
    }
}
