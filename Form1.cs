using Minitimer.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace Minitimer {
    public partial class Form1 : Form {
        DateTime Deadline { get; set; } = DateTime.MinValue;


        const string DefaultTimeLabel = "00:00";
        string TimeLabel { get; set; } = DefaultTimeLabel;

        float TextFontSize {
            get {
                return 48.0f * DeviceDpi / 96.0f;
            }
        }

        int BorderSize {
            get {
                return (int)Math.Ceiling(2.0f * DeviceDpi / 96.0f);
            }
        }

        Font TextFont;
        readonly BufferedGraphicsContext graphicsContext = BufferedGraphicsManager.Current;
        BufferedGraphics graphics = null;

        public Form1() {
            InitializeComponent();
            UpdateFont();
            RepositionForm();
            RecreateBuffer();
            PaintTimer(TimeLabel);
        }

        private void RepositionForm() {
            var currentScreen = Screen.FromControl(this);
            var realX = currentScreen.WorkingArea.Width - Width - LogicalToDeviceUnits(8);
            var realY = currentScreen.WorkingArea.Height - Height - LogicalToDeviceUnits(8);
            Location = new Point(realX, realY);
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left && e.Clicks < 2) {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, NativeMethods.WM_NCLBUTTONDOWN, (UIntPtr)NativeMethods.HT_CAPTION, IntPtr.Zero);
            }
        }

        private void Form1_Activated(object sender, EventArgs e) {
            Opacity = 1;
        }

        private void Form1_Deactivate(object sender, EventArgs e) {
            try {
                Opacity = 0.5;
            } catch { }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            Settings.Default.Save();
            Deactivate -= Form1_Deactivate;
            Hide();
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e) {
            AddTime(30);
        }

        private void AddTime(double time) {
            var now = DateTime.Now;
            if (time > 0 && now > Deadline) {
                Deadline = now + TimeSpan.FromSeconds(time);
            } else if (time > 0 || Deadline > now) {
                Deadline += TimeSpan.FromSeconds(time);
            }
            DoUpdate();
            if (Deadline > now) {
                timer1.Start();
            } else {
                SystemSounds.Beep.Play();
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            DoUpdate();
        }

        private void PaintTimer(string value) {
            var g = graphics.Graphics;
            g.FillRectangle(SystemBrushes.ControlText, 0, 0, Width, Height);
            var textBound = new Rectangle(BorderSize, BorderSize, Width - BorderSize * 2, Height - BorderSize * 2);
            g.FillRectangle(SystemBrushes.Control, textBound);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(g, value, TextFont, textBound, SystemColors.ControlText, Color.Transparent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Invalidate();
        }

        private void DoUpdate() {
            var now = DateTime.Now;
            if (Deadline > now) {
                var rem = TimeSpan.FromSeconds(Math.Ceiling((Deadline - now).TotalSeconds));
                TimeLabel = rem.ToString("mm\\:ss");
                PaintTimer(TimeLabel);
            } else {
                TimeLabel = DefaultTimeLabel;
                PaintTimer(TimeLabel);
                if (timer1.Enabled) {
                    timer1.Stop();
                    SystemSounds.Beep.Play();
                    if (Settings.Default.CloseOnFinish) {
                        Close();
                    }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                Close();
            } else if (e.KeyCode == Keys.Space) {
                AddTime(1);
            } else if (e.KeyCode == Keys.Back) {
                AddTime(-1);
            }
        }

        private void closeOnFinishToolStripMenuItem_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.CloseOnFinish = closeOnFinishToolStripMenuItem.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e) {
            RecreateBuffer();
            PaintTimer(TimeLabel);
        }

        private void RecreateBuffer() {
            // Re-create the graphics buffer for a new window size.
            graphicsContext.MaximumBuffer = new Size(this.Width + 1, this.Height + 1);
            if (graphics != null) {
                graphics.Dispose();
                graphics = null;
            }
            graphics = graphicsContext.Allocate(this.CreateGraphics(), new Rectangle(0, 0, this.Width, this.Height));
        }

        private void Form1_DpiChanged(object sender, DpiChangedEventArgs e) {
            UpdateFont();
        }

        private void UpdateFont() {
            TextFont = new Font(FontFamily.GenericSansSerif, TextFontSize, GraphicsUnit.Point);
            var contentSize = TextRenderer.MeasureText(TimeLabel, TextFont, Size.Empty,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            contentSize.Width += BorderSize * 2;
            contentSize.Height += BorderSize * 2;
            Size = SizeFromClientSize(contentSize);
        }

        protected override void OnPaint(PaintEventArgs e) {
            graphics.Render(e.Graphics);
        }
    }
}
