using System;
using System.Windows.Forms;

namespace Server
{
    internal static class Program
    {
        public static ServerForm f;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerForm());
        }
    }
}
