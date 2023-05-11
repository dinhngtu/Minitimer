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
                var f = new Form1();
                var currentScreen = Screen.FromControl(f);
                var realX = currentScreen.WorkingArea.Width - f.Width - f.LogicalToDeviceUnits(8);
                var realY = currentScreen.WorkingArea.Height - f.Height - f.LogicalToDeviceUnits(8);
                f.Location = new Point(realX, realY);
                Application.Run(f);
            }
        }
    }
}
