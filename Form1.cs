using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minitimer {
    public partial class Form1 : Form {
        DateTime Deadline { get; set; } = DateTime.MinValue;

        public Form1() {
            InitializeComponent();
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
            Deactivate -= Form1_Deactivate;
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e) {
            var now = DateTime.Now;
            if (now > Deadline) {
                Deadline = now + TimeSpan.FromSeconds(30);
            } else {
                Deadline += TimeSpan.FromSeconds(30);
            }
            DoUpdate();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e) {
            DoUpdate();
        }

        private void DoUpdate() {
            var now = DateTime.Now;
            if (Deadline > now) {
                var rem = TimeSpan.FromSeconds(Math.Ceiling((Deadline - now).TotalSeconds));
                label1.Text = rem.ToString("mm\\:ss");
            } else {
                label1.Text = "00:00";
                timer1.Stop();
                SystemSounds.Beep.Play();
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }
    }
}
