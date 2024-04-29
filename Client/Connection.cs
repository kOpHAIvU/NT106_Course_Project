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
        //Khi nhấn nút Connect 
        private void btnConnect_Click(object sender, EventArgs e)
        {
            //Lấy giá trị cổng từ ô insertPort và gán cho biến Port trong class ConnectionOptions
            ConnectionOptions.Port = Convert.ToInt32(insertPort.Text);
            // Lấy địa chỉ IP từ ô insertIP và gán cho biến IP trong class ConnectionOptions
            ConnectionOptions.IP = insertIP.Text;
            //Gắn cho DialogResult kết quả OK 
            DialogResult = DialogResult.OK;
            Close();
        }
        private void returnBtn_Click(object sender, EventArgs e)
        {
            //Khi nhấn vào nút trở lại thì gắn cho DialogResult kết quả Cancel 
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
