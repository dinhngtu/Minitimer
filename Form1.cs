using Minitimer.Properties;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace Minitimer {
    public partial class Form1 : Form {
        const string DefaultTimeLabel = "00:00";

        DateTime deadline = DateTime.MinValue;
        string timeLabel = DefaultTimeLabel;
        int borderSize;
        float textFontSize;
        Font textFont;
        readonly BufferedGraphicsContext graphicsContext = BufferedGraphicsManager.Current;
        BufferedGraphics graphics = null;

        readonly WaveOutEvent waveOut = new WaveOutEvent();
        readonly ConcurrentQueue<Action> onPlaybackStoppedActions = new ConcurrentQueue<Action>();
        readonly ISampleProvider sound = MakeSound();

        public Form1() {
            InitializeComponent();
            waveOut.PlaybackStopped += OnPlaybackStopped;
            UpdateSizes();
            RepositionForm(true);
            RecreateBuffer();
            DoPaint();
        }

        private void RepositionForm(bool force) {
            SuspendLayout();
            var currentScreen = Screen.FromControl(this);
            var x = Location.X;
            var rx = currentScreen.WorkingArea.Width - Width - LogicalToDeviceUnits(8);
            if (force || x + Width > rx) {
                x = rx;
            }
            var y = Location.Y;
            var ry = currentScreen.WorkingArea.Height - Height - LogicalToDeviceUnits(8);
            if (force || y + Height > ry) {
                y = ry;
            }
            Location = new Point(x, y);
            ResumeLayout();
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

        private static ISampleProvider MakeSound() {
            var beep = new SignalGenerator() {
                Type = SignalGeneratorType.Triangle,
                Frequency = 2000,
                Gain = 1.0,
            };
            var silence = new SilenceProvider(beep.WaveFormat).ToSampleProvider();
            var begin = beep.Take(TimeSpan.FromMilliseconds(80.0));
            for (int i = 0; i < 2; i++)
                begin = begin.FollowedBy(silence.Take(TimeSpan.FromMilliseconds(40.0))).FollowedBy(beep.Take(TimeSpan.FromMilliseconds(80.0)));
            return begin;
        }

        private void PlaySound() {
            waveOut.Init(sound);
            waveOut.Play();
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e) {
            while (onPlaybackStoppedActions.TryDequeue(out var action))
                this.Invoke(action);
        }

        private void AddTime(double time) {
            timer1.Stop();
            var now = DateTime.Now;
            var rem = deadline - now;
            if (rem < TimeSpan.Zero) {
                rem = TimeSpan.Zero;
            }
            rem += TimeSpan.FromSeconds(time);
            if (rem > TimeSpan.FromSeconds(3599.0)) {
                rem = TimeSpan.FromSeconds(3599.0);
            }
            deadline = now + TimeSpan.FromSeconds(Math.Ceiling(rem.TotalSeconds));
            DoUpdate();
            if (deadline > now) {
                timer1.Start();
            } else {
                SystemSounds.Beep.Play();
            }
        }

        private void timer1_Tick(object sender, EventArgs e) {
            DoUpdate();
        }

        private void DoPaint() {
            var g = graphics.Graphics;
            g.FillRectangle(SystemBrushes.ControlText, 0, 0, Width, Height);
            var textBound = new Rectangle(borderSize, borderSize, Width - borderSize * 2, Height - borderSize * 2);
            g.FillRectangle(SystemBrushes.Control, textBound);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            TextRenderer.DrawText(g, timeLabel, textFont, textBound, SystemColors.ControlText, Color.Transparent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Invalidate();
        }

        private void DoUpdate() {
            var now = DateTime.Now;
            if (deadline > now) {
                var rem = TimeSpan.FromSeconds(Math.Ceiling((deadline - now).TotalSeconds));
                timeLabel = rem.ToString("mm\\:ss");
                DoPaint();
            } else {
                timeLabel = DefaultTimeLabel;
                DoPaint();
                if (timer1.Enabled) {
                    timer1.Stop();
                    if (Settings.Default.CloseOnFinish) {
                        onPlaybackStoppedActions.Enqueue(() => Close());
                    }
                    PlaySound();
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                Close();
            } else if (e.KeyCode == Keys.Space) {
                if (e.Control)
                    AddTime(60);
                else
                    AddTime(1);
            } else if (e.KeyCode == Keys.Back) {
                if (e.Control)
                    AddTime(-60);
                else
                    AddTime(-1);
            }
        }

        private void closeOnFinishToolStripMenuItem_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.CloseOnFinish = closeOnFinishToolStripMenuItem.Checked;
        }

        private void Form1_Resize(object sender, EventArgs e) {
            RecreateBuffer();
            DoPaint();
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
            UpdateSizes();
        }

        private void UpdateSizes() {
            borderSize = (int)Math.Ceiling(2.0f * (DeviceDpi / 96.0f));
            textFontSize = Settings.Default.TextSize * (DeviceDpi / 96.0f);
            textFont = new Font(FontFamily.GenericSansSerif, textFontSize, GraphicsUnit.Point);
            var contentSize = TextRenderer.MeasureText(timeLabel, textFont, Size.Empty,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            contentSize.Width += borderSize * 2;
            contentSize.Height += borderSize * 2;
            Size = SizeFromClientSize(contentSize);
        }

        protected override void OnPaint(PaintEventArgs e) {
            graphics.Render(e.Graphics);
        }

        private void smallToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.TextSize = 32.0f;
            UpdateSizes();
            RepositionForm(false);
        }

        private void mediumToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.TextSize = 48.0f;
            UpdateSizes();
            RepositionForm(false);
        }

        private void largeToolStripMenuItem_Click(object sender, EventArgs e) {
            Settings.Default.TextSize = 64.0f;
            UpdateSizes();
            RepositionForm(false);
        }
    }
}
