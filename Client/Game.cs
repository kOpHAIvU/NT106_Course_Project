using System;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class Game : Form
    {
        //Tạo TcpClient để kết nối Server 
        private static TcpClient Client = new TcpClient();
        //Tạo luồng mạng để gửi nhận dữ liệu 
        private static NetworkStream Stream;
        //Kiểm tra người chơi đỏ/ xanh đã kết nối hay chưa 
        private bool RedConnected, BlueConnected;
        //Lưu giá trị của Xúc xắc, vị trí trên bàn cờ, ID của người chơi, 
        private static int Dice, CurrentPosition, CurrentPlayerId, RedDotsNameSplitter, BlueDotsNameSplitter;
        //Lưu thông tin của người chơi 
        private readonly Player[] Players = new Player[2];
        //Lưu thông tin của các ô trên bàn cờ 
        private readonly Property[] Properties = new Property[40];
        //Chứa hình ảnh của các ô 
        private readonly PictureBox[] Tile;
        private readonly int[] Opportunity = { -100, 100, -150, 150, -200, 200 };

        //Các nhà đã được mua
        private class Property
        {
            //Kiểm tra đã mua hoặc đã sở hữu chưa
            public bool Buyable, Owned;
            public string Color, Name;
            public int Price, Rent;
        }
        //Thông tin 1 người chơi 
        private class Player
        {
            //Cho biết số dư, số lượng tài sản, số lần vào tù, vị trí hiện tại 
            public int Balance = 1500, NumberOfPropertiesOwned, Jail, Position;
            //Tình trạng giam giữ/ thua cuộc
            public bool InJail, Loser;
            //ID của tài sản 
            public readonly int[] PropertiesOwned = new int[40];
        }
        //Nhận dữ liệu trên Server
        private class ReceivedMessage
        {
            public bool InJail, Loser;
            public int EndPosition, Balance, Jail;
            public readonly int[] PropertiesOwned = new int[40];
        }
        public Game()
        {
            InitializeComponent();
            //Nếu nhiều người chơi 
            if (Gamemodes.Multiplayer)
                try
                {
                    //Hiển thị form kết nối
                    Connection connection = new Connection();
                    connection.ShowDialog();
                    //Nếu chọn Cancel thì hủy kết nối ròi quay về MainMenu chính 
                    if (connection.DialogResult is DialogResult.Cancel)
                    {
                        MainMenu mainMenu = new MainMenu();
                        mainMenu.ShowDialog();
                        Disconnect();
                    }
                    //Hiển thị thông điệp là đang đợi người chơi thứ 2 nên vô hiệu hóa các nút chơi
                    currentPlayersTurn_textbox.Text = "Chờ đợi người chơi thứ hai...";
                    throwDiceBtn.Enabled = false;
                    buyBtn.Enabled = false;
                    endTurnBtn.Enabled = false;
                    //Kết nối tới Server
                    try
                    {
                        Client = new TcpClient();
                        Client.Connect(ConnectionOptions.IP, ConnectionOptions.Port);
                        Stream = Client.GetStream();
                    }
                    catch
                    {
                        MessageBox.Show("Không thể kết nối tới server."
                                        + Environment.NewLine
                                        + "Server không hoạt động");
                        Disconnect();
                    }
                    //Tạo luồng nhận dữ liệu từ Server 
                    Thread receiveThread = new Thread(ReceiveMessage);
                    receiveThread.Start();
                    //Gửi các thông điẹp cho server biết Người chơi mới vào 
                    Stream.Write(
                        Encoding.Unicode.GetBytes("Người chơi mới đã vào"),
                        0,
                        Encoding.Unicode.GetBytes("Người chơi mới đã vào").Length);

                    //Hiển thị Form chọn màu 
                    ColorChoosing colorChoosing = new ColorChoosing();
                    colorChoosing.ShowDialog();
                    //Nếu chọn Cancel thì hủy kết nối ròi quay về MainMenu chính 
                    if (colorChoosing.DialogResult is DialogResult.Cancel)
                    {
                        MainMenu mainMenu = new MainMenu();
                        mainMenu.ShowDialog();
                        Disconnect();
                    }
                    //Gửi tên  người chơi đến server
                    Stream.Write(
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName),
                        0,
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName).Length);

                    Task.Delay(100);
                    //Gửi thông điệp phòng của client
                    string request = $"/join {ConnectionOptions.Room}";
                    Stream.Write(
                                Encoding.Unicode.GetBytes($"/join {ConnectionOptions.Room}"),
                                0,
                                Encoding.Unicode.GetBytes($"/join {ConnectionOptions.Room}").Length);
                    //Xác định người chơi hiện tại và đánh dấu họ đã kết nối 
                    if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Đỏ\s*\(\s*(\d+)\s*\)"))
                    {
                        RedConnected = true;
                        CurrentPlayerId = 0;
                    }
                    else if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Xanh\s*\(\s*(\d+)\s*\)"))
                    {
                        BlueConnected = true;
                        CurrentPlayerId = 1;
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            //Tạo các ô trên bàn cờ và người chơi 
            #region Creating tiles and players
            Tile = new[]
            {
                tile0, tile1, tile2, tile3, tile4, tile5, tile6, tile7, tile8, tile9, tile10,
                tile11, tile12, tile13, tile14, tile15, tile16, tile17, tile18, tile19, tile20,
                tile21, tile22, tile23, tile24, tile25, tile26, tile27, tile28, tile29, tile30,
                tile31, tile32, tile33, tile34, tile35, tile36, tile37, tile38, tile39
            };
            CreateTile("GO", false, "Null", 0, 0);
            CreateTile("Phú Lâm", true, "Purple", 60, 1);
            CreateTile("Khí vận", false, "Opportunity", 0, 2);
            CreateTile("Nhà bè Phú Xuân", true, "Purple", 60, 3);
            CreateTile("Thuế lợi tức", false, "White", 0, 4);
            CreateTile("Bến xe Lục Tỉnh", true, "Station", 200, 5);
            CreateTile("Thị Nghè", true, "Turquoise", 100, 6);
            CreateTile("Cơ hội", false, "Opportunity", 0, 7);
            CreateTile("Tân Định", true, "Turquoise", 100, 8);
            CreateTile("Bến Chương Dương", true, "Turquoise", 120, 9);
            CreateTile("Thăm tù", false, "Null", 0, 10);
            CreateTile("Phan Đình Phùng", true, "Pink", 140, 11);
            CreateTile("Công ty điện lực", false, "White", 0, 12);
            CreateTile("Trịnh Minh Thế", true, "Pink", 140, 13);
            CreateTile("Lý Thái Tổ", true, "Pink", 160, 14);
            CreateTile("Bến xe Lam Chợ Lớn", true, "Station", 200, 15);
            CreateTile("Đại lộ Hùng Vương", true, "Orange", 180, 16);
            CreateTile("Khí vận", false, "Opportunity", 0, 17);
            CreateTile("Gia Long", true, "Orange", 180, 18);
            CreateTile("Bến Bạch Đằng", true, "Orange", 200, 19);
            CreateTile("Sân bay", false, "Null", 0, 20);
            CreateTile("Đường Công Lý", true, "Red", 220, 21);
            CreateTile("Cơ hội", false, "Opportunity", 0, 22);
            CreateTile("Đại lộ thống nhất", true, "Red", 220, 23);
            CreateTile("Đại lộ Cộng Hòa", true, "Red", 240, 24);
            CreateTile("Bến xe An Đông", false, "Station", 200, 25);
            CreateTile("Đại lộ Hồng Thập Tự", true, "Yellow", 260, 26);
            CreateTile("Đại lộ Hai Bà Trưng", true, "Yellow", 260, 27);
            CreateTile("Công ty thủy cục", false, "White", 0, 28);
            CreateTile("Xa lộ Biên Hòa", true, "Yellow", 280, 29);
            CreateTile("VÔ TÙ", false, "Null", 0, 30);
            CreateTile("Phan Thanh Giảm", true, "Green", 300, 31);
            CreateTile("Lê Văn Duyệt", true, "Green", 300, 32);
            CreateTile("Khí vận", false, "Opportunity", 0, 33);
            CreateTile("Nguyễn Thái Học", true, "Green", 320, 34);
            CreateTile("Tân Kì Tân Quý", false, "White", 0, 35);
            CreateTile("Cơ hội", false, "Opportunity", 0, 36);
            CreateTile("Nha Trang", true, "Blue", 350, 37);
            CreateTile("Thuế lương bổng", false, "White", 0, 38);
            CreateTile("Cố Đô Huế", true, "Blue", 400, 39);

            Players[0] = new Player();
            Players[1] = new Player();
            #endregion
            //Cập nhật giao diện người chơi 
            UpdatePlayersStatusBoxes();
            buyBtn.Enabled = false;
        }

        //Tạo ô cờ gồm tên, màu, có thể mua được, giá, vị trí 
        private void CreateTile(string tileName, bool tileBuyable, string tileColor, int tilePrice, int tilePosition)
        {
            //Tạo các đối tượng và gán giá trị 
            Property property = new Property
            {
                Name = tileName,
                Color = tileColor,
                Buyable = tileBuyable,
                Price = tilePrice
            };
            //Gắn ô trên bàn cờ vào vị trí tương ứng trong mảng Properties 
            Properties[tilePosition] = property;
        }
        //Chuyển danh sách tài sản thành 1 chuỗi để hiển thị 
        private string PropertiesToString(int[] propertyList)
        {
            var tempString = "";
            //Chạy qua ds tài sản, sau đó thêm tên, màu vào chuỗi 
            for (var i = 0; i < 40; i++)
                if (propertyList[i] != 0)
                    tempString = tempString + Properties[propertyList[i]].Name + ", " + Properties[propertyList[i]].Color + "\n";
            return tempString;
        }
        //Cập nhật thông tin về người chơi trên giao diện 
        private void UpdatePlayersStatusBoxes()
        {
            redPlayerStatusBox_richtextbox.Text = "Người chơi Đỏ" + "\n"
                + "Tiền còn lại: " + Players[0].Balance + "\n"
                + PropertiesToString(Players[0].PropertiesOwned);
            bluePlayerStatusBox_richtextbox.Text = "Người chơi Xanh" + "\n"
                + "Tiền còn lại: " + Players[1].Balance + "\n"
                + PropertiesToString(Players[1].PropertiesOwned);
        }
        //Thay đổi số dư và cập nhật lên giao diện 
        private void ChangeBalance(Player player, int cashChange)
        {
            player.Balance += cashChange;
            UpdatePlayersStatusBoxes();
        }
        //Đưa người chơi vào tù 
        private void InJail(int currentPlayer)
        {
            //Tăng số lần vào tù 
            Players[currentPlayer].Jail += 1;
            //Vô hiệu hóa các nút khi vào tù 
            buyBtn.Enabled = false;
            throwDiceBtn.Enabled = false;
            //Thông báo tình trạng người chơi 
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text =
                        "Đỏ, bạn đang ở tù!\r\nLượt của bạn sẽ bị bỏ qua và tới lượt kế. "; break;
                case 1:
                    currentPlayersTurn_textbox.Text =
                        "Xanh, bạn đang ở tù.!\r\nLượt của bạn sẽ bị bỏ qua và tới lượt kế. ";
                    break;
            }
            //Nếu người chơi đã vào tù 3 lần thì thả người chơi ra 
            if (Players[currentPlayer].Jail != 3) return;
            //Sau khi thả người chơi thì hiển thị thông báo trên dao diện
            Players[currentPlayer].InJail = false;
            Players[currentPlayer].Jail = 0;
            throwDiceBtn.Enabled = true;
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text =
                        "Đỏ, bạn đã được thả! ";
                    break;
                case 1:
                    currentPlayersTurn_textbox.Text =
                        "Xanh, bạn đã được thả! ";
                    break;
            }
        }
        //Trả về số tiền thuê tại vị trí hiện tại dựa trên giá trị của xúc xắc 
        private int GetRent(int dice)
        {
            //Xác định loại tài sản tại vị trí hiện tại và tính tiền thuê tương ứng 
            switch (Properties[CurrentPosition].Color)
            {
                case "Null":
                    Properties[CurrentPosition].Rent = 0;
                    break;
                case "Station":
                    Properties[CurrentPosition].Rent = dice * 20;
                    break;
                case "White":
                    Properties[CurrentPosition].Rent = 0;
                    break;
                case "Opportunity ":
                    Properties[CurrentPosition].Rent = 0;
                    break;
                case "Purple":
                    Properties[CurrentPosition].Rent = 60;
                    break;
                case "Turquoise":
                    Properties[CurrentPosition].Rent = 120;
                    break;
                case "Pink":
                    Properties[CurrentPosition].Rent = 160;
                    break;
                case "Orange":
                    Properties[CurrentPosition].Rent = 200;
                    break;
                case "Red":
                    Properties[CurrentPosition].Rent = 240;
                    break;
                case "Yellow":
                    Properties[CurrentPosition].Rent = 280;
                    break;
                case "Green":
                    Properties[CurrentPosition].Rent = 320;
                    break;
                case "Blue":
                    Properties[CurrentPosition].Rent = 400;
                    break;
            }
            return Properties[CurrentPosition].Rent;
        }
        //Vẽ hình tròn đại diện cho vị trí người chơi trên bàn cờ 
        private void DrawCircle(int position, int playerId)
        {
            //Lấy tọa độ x, y của các ô trên bàn cờ 
            int x = Tile[position].Location.X, y = Tile[position].Location.Y;
            //Tạo hình tròn đại diện cho người chơi và đặt tọa độ và hình ảnh tương ứng 
            switch (playerId)
            {
                //Người chơi màu đỏ 
                case 0:
                    {
                        var redMarker = new PictureBox
                        {
                            Size = new Size(30, 30),
                            Name = "redMarker" + RedDotsNameSplitter,
                            BackgroundImage = redDot_picturebox.BackgroundImage,
                            BackColor = Color.Transparent,
                            Left = x,
                            Top = y
                        };
                        //Thêm hình tròn vào danh sách các control và đưa lên phía trước 
                        Controls.Add(redMarker);
                        redMarker.BringToFront();
                        //Tăng biến đếm tên của hình tròn đỏ 
                        RedDotsNameSplitter++;
                        break;
                    }
                case 1:
                    {
                        var blueMarker = new PictureBox
                        {
                            Size = new Size(30, 30),
                            Name = "blueMarker" + BlueDotsNameSplitter,
                            BackgroundImage = blueDot_picturebox.BackgroundImage,
                            BackColor = Color.Transparent,
                            Left = x,
                            Top = y
                        };
                        Controls.Add(blueMarker);
                        blueMarker.BringToFront();
                        //Tăng biến đếm tên của hình tròn xanh 
                        BlueDotsNameSplitter++;
                        break;
                    }
            }
        }
        //Nhận các tin nhắn từ server và xử lý
        private void ReceiveMessage()
        {
            //Lặp vô hạn để liên tục nhận tin nhắn từ máy chủ 
            while (true)
                try
                {
                    //Tạo mảng byte để chứa dữ liệu từ máy chủ 
                    byte[] data = new byte[256];
                    //Tạo một StringBuilder để xây dựng chuỗi tù ư dũ liệu nhận được 
                    StringBuilder builder = new StringBuilder();
                    //Đọc dữ liệu từ luồng và thêm vào StringBuilder cho tới khi không còn dữ liệu khả dụng 
                    do
                    {
                        var bytes = Stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    } while (Stream.DataAvailable);
                    //Chuyển StringBuilder thành chuỗi 
                    String message = builder.ToString();
                    //Xử lý các loại tin nhắn từ máy chủ
                    string[] parts = message.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

                    if (Regex.IsMatch(message, @"Cả\s+2\s+người\s+chơi\s+đã\s+kết\s+nối:\s+\d+") && parts[parts.Length-1] == ConnectionOptions.Room)
                    {
                        if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Đỏ\s*\(\s*(\d+)\s*\)"))
                        {
                            currentPlayersTurn_textbox.Text = "Tung xúc sắc để bất đầu trò chơi";
                            throwDiceBtn.Enabled = true;
                            buyBtn.Enabled = false;
                            endTurnBtn.Enabled = true;
                        }
                        if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Xanh\s*\(\s*(\d+)\s*\)"))
                        {
                            currentPlayersTurn_textbox.Text = "Đỏ đang thực hiện lượt chơi. Chờ...";
                        }
                    }

                    //Khi người chơi màu đỏ đã kết nối 
                    else if (Regex.IsMatch(message, @"Đỏ\s*\(\s*(\d+)\s*\)\s*đã kết nối") && parts[1] == ConnectionOptions.Room)
                    {
                        RedConnected = true;
                        ConnectionOptions.NameRedIsTaken = true;
                        // Kiểm tra xem người chơi màu xanh có kết nối không và gửi thông báo nếu cả hai đã kết nối
                        if (!BlueConnected) continue;
                        Stream.Write(Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room), 0, Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room).Length);
                    }

                    //Khi người chơi màu xanh đã kết nối 
                    else if (Regex.IsMatch(message, @"Xanh\s*\(\s*(\d+)\s*\)\s*đã kết nối") && parts[1] == ConnectionOptions.Room)
                    {
                        BlueConnected = true;
                        ConnectionOptions.NameBlueIsTaken = true;
                        // Kiểm tra xem người chơi màu đỏ có kết nối không và gửi thông báo nếu cả hai đã kết nối
                        if (!RedConnected) continue;
                        Stream.Write(Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room), 0, Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room).Length);

                    }
                    else if (message == "Quân tốt Đỏ đã được chọn")
                    {
                        ConnectionOptions.NameRedIsTaken = true;
                    }
                    else if (message == "Quân tốt Xanh đã được chọn")
                    {
                        ConnectionOptions.NameBlueIsTaken = true;
                    }
                   
                    //Xử lý tin nhắn

                    if (message.Contains("Đỏ(") || message.Contains("Xanh("))
                    {
                        UpdateChatBox(message);
                    } 
                    
                    //Khi nhận được kết quả lượt đi 
                    //Xử lý thông tin nhận được và cập nhật kết quả cho người 
                    if (message.Contains("Kết quả lượt đi") && parts[0] == ConnectionOptions.Room)
                    {
                        var tempMessage = message;
                        var subString = string.Empty;
                        switch (CurrentPlayerId)
                        {
                            case 0:
                                subString = "Kết quả lượt đi của Xanh";
                                break;
                            case 1:
                                subString = "Kết quả lượt đi của Đỏ";
                                break;
                        }
                        tempMessage = tempMessage.Replace(subString, "");

                        currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                        {
                            currentPlayersTurn_textbox.Text = "Lượt của bạn";
                        });

                        throwDiceBtn.Enabled = true;
                        buyBtn.Enabled = false;
                        endTurnBtn.Enabled = true;
                        ReceivedMessage receivedMessage = new ReceivedMessage();

                        //Lấy các trạng thái sau một lượt đi
                        //Vị trí sau lượt đi
                        String stringPosition = tempMessage.Split('~')[1];
                        receivedMessage.EndPosition = Convert.ToInt32(stringPosition);

                        //Tiền sau lượt đi
                        String stringBalance = tempMessage.Split('~')[2];
                        receivedMessage.Balance = Convert.ToInt32(stringBalance);

                        //đang trong tù hay không?
                        String stringInJail = tempMessage.Split('~')[3];
                        switch (stringInJail)
                        {
                            case "TRUE":
                                receivedMessage.InJail = true;
                                break;
                            case "FALSE":
                                receivedMessage.InJail = false;
                                break;
                        }

                        //Sau lượt đi có vào tù hay không?
                        String stringJail = tempMessage.Split('~')[4];
                        receivedMessage.Jail = Convert.ToInt32(stringJail);

                        //Tài sản (đất) hiện có
                        String stringPropertiesOwned = tempMessage.Split('~')[5];
                        if (stringPropertiesOwned != "NULL")
                        {
                            //Lấy mã số của các nhà được sở hữu
                            int[] tempArrayOfPropertiesOwned = stringPropertiesOwned
                                .Split(' ')
                                .Where(x => !string
                                .IsNullOrWhiteSpace(x))
                                .Select(x => int.Parse(x))
                                .ToArray();
                            for (int k = 0; k < tempArrayOfPropertiesOwned.Length; k++) 
                                receivedMessage.PropertiesOwned[k] = tempArrayOfPropertiesOwned[k];
                        }

                        //Kiểm tra người chơi kia có thua không
                        String stringLoser = tempMessage.Split('~')[6];
                        switch (stringLoser)
                        {
                            case "TRUE":
                                receivedMessage.Loser = true;
                                break;
                            case "FALSE":
                                receivedMessage.Loser = false;
                                break;
                        }

                        if (Players[CurrentPlayerId].InJail)
                        {
                            CurrentPosition = 10;
                            MoveIcon(CurrentPosition);
                            Players[CurrentPlayerId].Position = CurrentPosition;
                            InJail(CurrentPlayerId);
                        }

                        //Kiểm tra xem ai thắng
                        if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance < 0) 
                            Lose();
                        int count = 0;
                        for (int u = 0; u < 2; u++)
                        {
                            if (Players[u].Loser) 
                                count++;
                            if (Players[CurrentPlayerId].Loser || count < 1) 
                                continue;
                            currentPlayersTurn_textbox.Text = "Bạn thắng!";
                            switch (CurrentPlayerId)
                            {
                                case 0:
                                    if (MessageBox.Show("Đỏ, bạn đã thắng", "Thông báo", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                                    break;
                                case 1:
                                    if (MessageBox.Show("Xanh, bạn đã thắng", "Thông báo", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                                    break;
                            }
                        }

                        //Cập nhật trạng thái của biến Players
                        switch (CurrentPlayerId)
                        {
                            case 0:
                                CurrentPlayerId = 1;
                                MoveIcon(receivedMessage.EndPosition);
                                Players[CurrentPlayerId].Position = receivedMessage.EndPosition;
                                Players[CurrentPlayerId].Balance = receivedMessage.Balance;
                                Players[CurrentPlayerId].InJail = receivedMessage.InJail;
                                Players[CurrentPlayerId].Jail = receivedMessage.Jail;

                                if (Players[CurrentPlayerId].InJail) 
                                    InJail(CurrentPlayerId);
                                int i = 0;

                                foreach (var item in receivedMessage.PropertiesOwned)
                                {
                                    Players[CurrentPlayerId].PropertiesOwned[i] = item;
                                    i++;
                                }

                                foreach (var item in Players[CurrentPlayerId].PropertiesOwned)
                                    if (item != 0)
                                    {
                                        Properties[item].Owned = true;
                                        Players[CurrentPlayerId].NumberOfPropertiesOwned++;
                                        currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                                        {
                                            DrawCircle(item, 1);
                                        });
                                    }

                                Players[CurrentPlayerId].Loser = receivedMessage.Loser;
                                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) 
                                    Lose();
                                CurrentPlayerId = 0;
                                UpdatePlayersStatusBoxes();
                                break;

                            case 1:
                                CurrentPlayerId = 0;
                                MoveIcon(receivedMessage.EndPosition);
                                Players[CurrentPlayerId].Position = receivedMessage.EndPosition;
                                Players[CurrentPlayerId].Balance = receivedMessage.Balance;
                                Players[CurrentPlayerId].InJail = receivedMessage.InJail;
                                Players[CurrentPlayerId].Jail = receivedMessage.Jail;
                                if (Players[CurrentPlayerId].InJail) 
                                    InJail(CurrentPlayerId);

                                int k = 0;
                                foreach (var item in receivedMessage.PropertiesOwned)
                                {
                                    Players[CurrentPlayerId].PropertiesOwned[k] = item;
                                    k++;
                                }

                                foreach (var item in Players[CurrentPlayerId].PropertiesOwned)
                                    if (item != 0)
                                    {
                                        Properties[item].Owned = true;
                                        Players[CurrentPlayerId].NumberOfPropertiesOwned++;
                                        currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                                        {
                                            DrawCircle(item, 0);
                                        });
                                    }

                                Players[CurrentPlayerId].Loser = receivedMessage.Loser;
                                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) 
                                    Lose();
                                CurrentPlayerId = 1;
                                UpdatePlayersStatusBoxes();
                                break;
                        }
                    }
                    //Cập nhật số tiền cho người chơi 
                    if (message.Contains("Trả tiền thuê nhà cho Đỏ: ") && parts[0] == ConnectionOptions.Room)
                    {
                        string sumOfRentString = parts[parts.Length-1];
                        int sumOfRent = Convert.ToInt32(sumOfRentString);
                        ChangeBalance(Players[1], -sumOfRent);
                        ChangeBalance(Players[0], sumOfRent);
                        MessageBox.Show("Xanh trả tiền thuê nhà cho Đỏ: : " + sumOfRent);
                    }
                    else if (message.Contains("Trả tiền thuê nhà cho Xanh: ")&& parts[0] == ConnectionOptions.Room)
                    {
                        string sumOfRentString = parts[parts.Length - 1];
                        int sumOfRent = Convert.ToInt32(sumOfRentString);
                        ChangeBalance(Players[0], -sumOfRent);
                        ChangeBalance(Players[1], sumOfRent);
                        MessageBox.Show("Đỏ trả tiền thuê nhà cho Xanh: " + sumOfRent);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection has been lost! " + ex.Message);
                    Disconnect();
                }
        }

        //Hàm được gọi khi người chơi thua cuộc
        private void Lose()
        {
            //Đánh dấu người chơi hiện tại thua cuộc 
            Players[CurrentPlayerId].Loser = true;
            //Vô hiệu hóa các nút chơi 
            throwDiceBtn.Enabled = false;
            buyBtn.Enabled = false;
            endTurnBtn.Enabled = false;
            //Hiển thị thông báo về sự thua cuộc của người chơi 
            switch (CurrentPlayerId)
            {
                case 0 when Players[0].Loser:
                    currentPlayersTurn_textbox.Text = "Đỏ, bạn đã thua!";
                    break;
                case 1 when Players[1].Loser:
                    currentPlayersTurn_textbox.Text = "Xanh, bạn đã thua!";
                    break;
            }
        }
        //Phương thức ngắt kết nối và thoát ứng dụng 
        private static void Disconnect()
        {
            //Đống luồng dữ liệu 
            Stream?.Close();
            //Đóng kết nối của client 
            Client?.Close();
            //Thoát khỏi ứng dụng
            Environment.Exit(0);
        }
        //Phương thức di chiển biểu tượng của người chơi
        private void MoveIcon(int position)
        {
            int x, y;
            switch (CurrentPlayerId)
            {
                //Lấy tọa độ mới cho biểu tượng màu đỏ/ xanh và di chuyển đến tọa độ mới 
                case 0:
                    x = Tile[position].Location.X;
                    y = Tile[position].Location.Y;
                    redPawnIcon.Location = new Point(x, y);
                    break;
                case 1:
                    x = Tile[position].Location.X;
                    y = Tile[position].Location.Y;
                    bluePawnIcon.Location = new Point(x, y);
                    break;
            }
        }

        private void sendBt_Click(object sender, EventArgs e)
        {
            string message = messageTb.Text.Trim();
            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Vui lòng điền vào TextBox trước khi gửi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Stream.Write(Encoding.Unicode.GetBytes("nhắn: " + message), 0, Encoding.Unicode.GetBytes("nhắn : " + message).Length);
            messageTb.Text = "";
        }

        private void UpdateChatBox(string message)
        {
            if (chatListBox.InvokeRequired)
            {
                chatListBox.Invoke(new Action<string>(UpdateChatBox), message);
            }
            else
            {
                chatListBox.Items.Add(message);
            }
        }



        //Animation di chuyển vị trí
        private async Task<int> MoveTileByTile(int from, int to)
        {
            // Nếu vị trí đích nhỏ hơn 40 (nằm trong phạm vi của bảng), di chuyển từ ô hiện tại đến ô đích
            if (to < 40)
            {
                for (var i = from; i <= to; i++)
                {
                    await Task.Delay(150);
                    MoveIcon(i);
                }
            }
            else
            {
                // Nếu vị trí đích lớn hơn hoặc bằng 40, di chuyển từ ô hiện tại đến ô cuối cùng (39),
                // sau đó di chuyển từ ô đầu tiên (0) đến ô đích với phần dư của vị trí đích sau khi chia cho 40
                for (var i = from; i <= 39; i++)
                {
                    await Task.Delay(150);
                    MoveIcon(i);
                }
                for (var i = 0; i <= to - 40; i++)
                {
                    await Task.Delay(150);
                    MoveIcon(i);
                }
            }
            return 1;
        }
        //Xử lý khi nút ném xúc xắc 
        private void ThrowDiceBtn_Click(object sender, EventArgs e)
        {
            //Hiển thị thông tin của người chơi hiện tại 
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text = "Lượt của người chơi Đỏ. ";
                    break;
                case 1:
                    currentPlayersTurn_textbox.Text = "Lượt của người chơi Xanh. ";
                    break;
            }
            //Tạo các biến để theo dõi việc đi qua các ô đặt biệt 
            bool visitedJailExploration = false
                , visitedTaxTile = false
                , visitedGo = false
                , visitedFreeParking = false
                , goingToJail = false
                , landedOpportunity = false;
            int OppResult = new int();
            //Cho phép người chơi mua bút 
            buyBtn.Enabled = true;
            //Cập nhật trạng thái của người chơi 
            UpdatePlayersStatusBoxes();

            //Ném xúc sắc 
            Random rand = new Random();
            int firstDice = rand.Next(1, 7);
            int secondDice = rand.Next(1, 7);
            Dice = firstDice + secondDice;
            //Hiển thị kết quả xức sắc 
            whatIsOnDices_textbox.Text = "Kết quả tung: " + firstDice + " và " + secondDice + ". Tổng: " + Dice + ". ";
            //vô hiệu hóa sau khi ném 
            throwDiceBtn.Enabled = false;
            //Lưu vị trí trước và sau khi ném
            int positionBeforeDicing = Players[CurrentPlayerId].Position;
            CurrentPosition = Players[CurrentPlayerId].Position + Dice;
            int positionAfterDicing = Players[CurrentPlayerId].Position + Dice;
            //Xử lý người chơi ở trong tù 
            if (Players[CurrentPlayerId].InJail) 
                InJail(CurrentPlayerId);

            //Tới các ô chức năng ở 4 góc
            switch (CurrentPosition)
            {
                case 0:
                    buyBtn.Enabled = false;
                    visitedGo = true;
                    break;
                case 10 when Players[CurrentPlayerId].InJail is false:
                    buyBtn.Enabled = false;
                    visitedJailExploration = true;
                    break;
                case 20:
                    buyBtn.Enabled = false;
                    visitedFreeParking = true;
                    break;
                case 30:
                    CurrentPosition = 10;
                    Players[CurrentPlayerId].InJail = true;
                    InJail(CurrentPlayerId);
                    goingToJail = true;
                    break;
            }

            if (CurrentPosition >= 40)
            {
                ChangeBalance(Players[CurrentPlayerId], 200);
                Players[CurrentPlayerId].Position = CurrentPosition - 40;
                CurrentPosition = Players[CurrentPlayerId].Position;
            }
            if (Properties[CurrentPosition].Color is "White")
            {   //Vào ô thuế
                ChangeBalance(Players[CurrentPlayerId], -200);
                buyBtn.Enabled = false;
                visitedTaxTile = true;
            } else if (Properties[CurrentPosition].Color is "Opportunity")
            {   //Vào cơ hội, khí vận
                Random random = new Random();
                int randNum = random.Next(0, Opportunity.Length);
                OppResult = Opportunity[randNum];
                ChangeBalance(Players[CurrentPlayerId], OppResult);
                landedOpportunity = true;
                buyBtn.Enabled = false;
            }            

            Players[CurrentPlayerId].Position = CurrentPosition;
            //Di chuyển tới tù
            switch (goingToJail)
            {
                case true:
                    MoveIcon(10);
                    break;
                case false:
                    _ = MoveTileByTile(positionBeforeDicing, positionAfterDicing);
                    break;
            }

            currentPositionInfo_richtextbox.Text = "Vị trí " + CurrentPosition;
            currentPositionInfo_richtextbox.AppendText("\r\n" + Properties[CurrentPosition].Name);
            currentPositionInfo_richtextbox.AppendText("\r\n" + "Giá " + Properties[CurrentPosition].Price);
            currentPositionInfo_richtextbox.AppendText("\r\n" + "Loại " + Properties[CurrentPosition].Color);

            if (visitedJailExploration) 
                currentPositionInfo_richtextbox.AppendText("\r\n" + "Bạn đang thăm tù. ");

            if (visitedTaxTile) 
                currentPositionInfo_richtextbox.AppendText("\r\n" + "Bạn đã nộp thuế");

            if (visitedGo) 
                currentPositionInfo_richtextbox.AppendText("\r\n" + "Nhận 200 sau khi đi qua ô \"GO\". ");

            if (visitedFreeParking) 
                currentPositionInfo_richtextbox.AppendText("\r\n" + "Thư giãn nào...");

            if (landedOpportunity)
                if (CurrentPosition == 2 || CurrentPosition == 17 || CurrentPosition == 33)
                    currentPositionInfo_richtextbox.AppendText("\r\n" + "Bạn nhận được " + Convert.ToString(OppResult) + " tại ô \"Khí vận\".");
                else
                    currentPositionInfo_richtextbox.AppendText("\r\n" + "Bạn nhận được " + Convert.ToString(OppResult) + " tại ô \"Cơ hội\".");

            if (goingToJail)
            {
                currentPositionInfo_richtextbox.AppendText("\r\n" + "Bạn đang ở tù.");
                switch (CurrentPlayerId)
                {
                    case 0:
                        currentPlayersTurn_textbox.Text = "Đỏ, bạn đang ở tù!\r\nLượt của bạn sẽ bị bỏ qua và tới lượt kế. ";
                        break;
                    case 1:
                        currentPlayersTurn_textbox.Text = "Xanh, bạn đang ở tù.!\r\nLượt của bạn sẽ bị bỏ qua và tới lượt kế. ";
                        break;
                }
            }

            currentPositionInfo_richtextbox.ScrollToCaret();
            if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) 
                Lose();

            int count = 0;
            for (int i = 0; i < 2; i++)
            {
                if (Players[i].Loser) 
                    count++;
                if (Players[CurrentPlayerId].Loser || count < 1) 
                    continue;
                currentPlayersTurn_textbox.Text = "Bạn thắng! Congratulations!";

                switch (CurrentPlayerId)
                {
                    case 0:
                        if (MessageBox.Show("Đỏ, bạn đã thắng", "Message", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                        break;
                    case 1:
                        if (MessageBox.Show("Xanh, bạn đã thắng", "Message", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                        break;
                }
            }

            //Nếu đất này chưa được mua hoặc bản thân đang sở hữu thì kết thúc hàm
            if (Players[CurrentPlayerId].PropertiesOwned[CurrentPosition] == CurrentPosition || !Properties[CurrentPosition].Owned) 
                return;
            //Nếu đất của đối phương thì sẽ thực hiện trả tiền cho đối phương
            buyBtn.Enabled = false;
            switch (CurrentPlayerId)
            {
                case 0:
                    ChangeBalance(Players[0], -GetRent(Dice));
                    ChangeBalance(Players[1], GetRent(Dice));
                    if (Gamemodes.Multiplayer)
                    {
                        string rentMessage = ConnectionOptions.Room + " Trả tiền thuê nhà cho Xanh: " + GetRent(Dice);
                        MessageBox.Show("Đỏ trả tiền thuê nhà cho Xanh: " + GetRent(Dice));
                        Stream.Write(Encoding.Unicode.GetBytes(rentMessage), 0, Encoding.Unicode.GetBytes(rentMessage).Length);
                    }
                    break;
                case 1:
                    ChangeBalance(Players[1], -GetRent(Dice));
                    ChangeBalance(Players[0], GetRent(Dice));
                    if (Gamemodes.Multiplayer)
                    {
                        string rentMessage = ConnectionOptions.Room + " Trả tiền thuê nhà cho Đỏ: : " + GetRent(Dice);
                        MessageBox.Show("Xanh trả tiền thuê nhà cho Đỏ: : " + GetRent(Dice));
                        Stream.Write(Encoding.Unicode.GetBytes(rentMessage), 0, Encoding.Unicode.GetBytes(rentMessage).Length);
                    }
                    break;
            }
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text = "Đỏ, bạn vừa vào đất của người chơi khác và phải đóng tiền ";
                    break;
                case 1:
                    currentPlayersTurn_textbox.Text = "Xanh, bạn vừa vào đất của người chơi khác và phải đóng tiền ";
                    break;
            }
            if (CurrentPosition is 5 || CurrentPosition is 15 || CurrentPosition is 25 || CurrentPosition is 35) 
                currentPlayersTurn_textbox.Text += Dice * 20;
            else currentPlayersTurn_textbox.Text += Properties[CurrentPosition].Rent;
        }

        private void BuyBtn_Click(object sender, EventArgs e)
        {
            if (Properties[CurrentPosition].Buyable && Properties[CurrentPosition].Owned is false)
                if (Players[CurrentPlayerId].Balance >= Properties[CurrentPosition].Price)
                {
                    ChangeBalance(Players[CurrentPlayerId], -Properties[CurrentPosition].Price);
                    //Lấy vị trí nhà mới
                    Players[CurrentPlayerId].PropertiesOwned[Players[CurrentPlayerId].NumberOfPropertiesOwned] = CurrentPosition;

                    Properties[CurrentPosition].Owned = true;
                    Players[CurrentPlayerId].NumberOfPropertiesOwned++;
                    UpdatePlayersStatusBoxes();
                    buyBtn.Enabled = false;
                    DrawCircle(CurrentPosition, CurrentPlayerId);
                }
                else 
                    currentPlayersTurn_textbox.Text = "Bạn không đủ tiền";
            else 
                currentPlayersTurn_textbox.Text = "Bạn không thể thực hiện hành động đó";

            if (Players[CurrentPlayerId].Balance < 0) 
                Lose();
        }

        private void QuitGameBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có muốn thoát", "Thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (Gamemodes.Multiplayer)
                    Stream.Write(
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " đã rời"),
                        0,
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " đã rời").Length);
                Disconnect();
                Application.Exit();
            }
        }

        private void EndTurnBtn_Click(object sender, EventArgs e)
        {
            if (Gamemodes.Multiplayer)
            {
                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) 
                    Lose();
                int count = 0;
                for (int i = 0; i < 2; i++)
                {
                    if (Players[i].Loser) 
                        count++;
                    if (Players[CurrentPlayerId].Loser || count < 1) 
                        continue;
                    currentPlayersTurn_textbox.Text = "Bạn thắng!";
                    switch (CurrentPlayerId)
                    {
                        case 0:
                            if (MessageBox.Show("Đỏ, bạn đã thắng", "Thông báo", MessageBoxButtons.OK) is DialogResult.OK) 
                                Application.Exit();
                            break;
                        case 1:
                            if (MessageBox.Show("Xanh, bạn đã thắng", "Thông báo", MessageBoxButtons.OK) is DialogResult.OK) 
                                Application.Exit();
                            break;
                    }
                }
                currentPositionInfo_richtextbox.Text = string.Empty;
                string turnLogString = string.Empty;
                switch (CurrentPlayerId)
                {
                    case 0:
                        turnLogString = ConnectionOptions.Room + " Kết quả lượt đi của Đỏ";
                        break;
                    case 1:
                        turnLogString = ConnectionOptions.Room + " Kết quả lượt đi của Xanh";
                        break;
                }
                turnLogString += CurrentPlayerId.ToString() + '~'
                    + Players[CurrentPlayerId].Position + '~' 
                    + Players[CurrentPlayerId].Balance + '~'
                    + Players[CurrentPlayerId].InJail + '~'
                    + Players[CurrentPlayerId].Jail + '~';
                foreach (var item in Players[CurrentPlayerId].PropertiesOwned)
                    if (item != 0)
                    {
                        turnLogString += item;
                        turnLogString += ' ';
                        turnLogString += ' ';
                    }
                if (turnLogString.Last() is '~') 
                    turnLogString += "NULL";
                turnLogString += '~' + Players[CurrentPlayerId].Loser.ToString();
                if (CurrentPlayerId is 0)
                {
                    currentPlayersTurn_textbox.Text = "Xanh đang thực hiện lượt chơi. Chờ...";
                    Stream.Write(Encoding.Unicode.GetBytes(turnLogString), 0, Encoding.Unicode.GetBytes(turnLogString).Length);
                } else {
                    currentPlayersTurn_textbox.Text = "Đỏ đang thực hiện lượt chơi. Chờ...";
                    Stream.Write(Encoding.Unicode.GetBytes(turnLogString), 0, Encoding.Unicode.GetBytes(turnLogString).Length);
                }
                throwDiceBtn.Enabled = false;
                buyBtn.Enabled = false;
                endTurnBtn.Enabled = false;
            }
            if (Gamemodes.Singleplayer)
            {
                CurrentPlayerId = CurrentPlayerId is 0 ? 1 : 0;
                switch (CurrentPlayerId)
                {
                    case 0:
                        currentPlayersTurn_textbox.Text = "Lượt của người chơi Đỏ. ";
                        break;
                    case 1:
                        currentPlayersTurn_textbox.Text = "Lượt của người chơi Xanh. ";
                        break;
                }
                throwDiceBtn.Enabled = true;
                buyBtn.Enabled = false;
                if (Players[CurrentPlayerId].InJail)
                {
                    CurrentPosition = 10;
                    MoveIcon(CurrentPosition);
                    Players[CurrentPlayerId].Position = CurrentPosition;
                    InJail(CurrentPlayerId);
                }
                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) Lose();
                var count = 0;
                for (var i = 0; i < 2; i++)
                {
                    if (Players[i].Loser) count++;
                    if (Players[CurrentPlayerId].Loser || count < 1) continue;
                    currentPlayersTurn_textbox.Text = "Bạn thắng!";
                    switch (CurrentPlayerId)
                    {
                        case 0:
                            if (MessageBox.Show("Đỏ, bạn đã thắng", "Message", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                            break;
                        case 1:
                            if (MessageBox.Show("Xanh, bạn đã thắng", "Message", MessageBoxButtons.OK) is DialogResult.OK) Application.Exit();
                            break;
                    }
                }
                currentPositionInfo_richtextbox.Text = string.Empty;
            }
        }
    }
}