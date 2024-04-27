using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Client
{
    public partial class Connection : Form
    {
        public Connection()
        {
            InitializeComponent();
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            ConnectionOptions.Port = Convert.ToInt32(insertPort.Text);
            ConnectionOptions.IP = insertIP.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
        private void returnBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
