using System;
using System.Windows.Forms;

namespace HDAArchiver
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application
                .SetCompatibleTextRenderingDefault(
                    false);

            string openFile = null;

            if (args.Length > 0 &&
                System.IO.File.Exists(args[0]))
            {
                openFile = args[0];
            }

            Application.Run(
                new MainForm(openFile));
        }
    }
}