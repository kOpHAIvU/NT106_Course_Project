using System;
using System.Windows.Forms;

namespace Client
{
    public partial class ColorChoosing : Form
    {
        public ColorChoosing()
        {
            Program.colorChoosing = this;
            InitializeComponent();

            //Thiết lập giá trị mặc định cho tbColor
            tbColor.Text = "Chưa được chọn";

            //Kiểm tra nếu màu đỏ/ xanh đã được chọn, thì không cho chọn màu đỏ/ xanh nữa
            if (ConnectionOptions.NameRedIsTaken) chooseRedPlayerBtn.Enabled = false;
            if (ConnectionOptions.NameBlueIsTaken) chooseBluePlayerBtn.Enabled = false;
        }
        private void connect_button_Click(object sender, EventArgs e)
        {
            switch (tbColor.Text)
            {
                //Nếu chọn màu đỏ 
                case "Đỏ":
                    //Gán tên người chơi là Red 
                    ConnectionOptions.PlayerName = "Đỏ";
                    //Cho biết tên màu đỏ đã được chọn
                    ConnectionOptions.NameRedIsTaken = true;
                    Close();
                    DialogResult = DialogResult.OK;
                    break;
                 //Nếu chọn màu xanh
                case "Xanh":
                    //Gán tên người chơi là Blue
                    ConnectionOptions.PlayerName = "Xanh";
                    //Cho biết tên màu xanh đã được chọn
                    ConnectionOptions.NameBlueIsTaken = true;
                    Close();
                    //Gắn cho DialogResult kết quả OK 
                    DialogResult = DialogResult.OK;
                    break;
                case "Chưa chọn":
                    //Nếu chưa chọn màu thì hiển thị thông báo yêu cầu chọn màu 
                    MessageBox.Show("Chọn màu!");
                    break;
            }
        }
        private void returnBtn_Click(object sender, EventArgs e)
        {
            //Khi nhấn vào nút trở lại thì gắn cho DialogResult kết quả Cancel 
            DialogResult = DialogResult.Cancel;
            Close();
        }

        //Xử lý sự kiện khi nhấn nút chọn màu đỏ
        private void chooseRedPlayerBtn_Click(object sender, EventArgs e)
        {
            //Cho tbColor hiển thị chữ "Red"
            tbColor.Text = "Red";
        }
        //Xử lý sự kiện khi nhấn nút chọn màu đỏ
        private void chooseBluePlayerBtn_Click(object sender, EventArgs e)
        {
            //Cho tbColor hiển thị chữ "Red"
            tbColor.Text = "Blue";
        }
    }
}
