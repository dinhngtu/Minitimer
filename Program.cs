using Minitimer.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minitimer {
    internal static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (var m = new Mutex(true, "{5F73BBBC-645D-4209-8EB7-A33A1C553B17}", out var created)) {
                if (!created) {
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Settings.Default.Upgrade();
                Application.Run(new Form1());
            }
        }
    }
}
