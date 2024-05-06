﻿using System;
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
        private static Socket serverSocket;
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
            public int Balance = 200, NumberOfPropertiesOwned, Jail, Position;
            //Tình trạng giam giữ/ thua cuộc
            public bool InJail;
            //ID của tài sản 
            public readonly int[] PropertiesOwned = new int[40];
        }
        //Nhận dữ liệu trên Server
        private class ReceivedMessage
        {
            public int EndPosition, Balance;
            public readonly int[] PropertiesOwned = new int[40];
        }
        public Game()
        {
            InitializeComponent();
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
            CreateTile("Công ty điện lực", true, "Station", 140, 12);
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
            CreateTile("Bến xe An Đông", true, "Station", 200, 25);
            CreateTile("Đại lộ Hồng Thập Tự", true, "Yellow", 260, 26);
            CreateTile("Đại lộ Hai Bà Trưng", true, "Yellow", 260, 27);
            CreateTile("Công ty thủy cục", true, "Station", 150, 28);
            CreateTile("Xa lộ Biên Hòa", true, "Yellow", 280, 29);
            CreateTile("VÔ TÙ", false, "Null", 0, 30);
            CreateTile("Phan Thanh Giảm", true, "Green", 300, 31);
            CreateTile("Lê Văn Duyệt", true, "Green", 300, 32);
            CreateTile("Khí vận", false, "Opportunity", 0, 33);
            CreateTile("Nguyễn Thái Học", true, "Green", 320, 34);
            CreateTile("Tân Kì Tân Quý", true, "Station", 400, 35);
            CreateTile("Cơ hội", false, "Opportunity", 0, 36);
            CreateTile("Nha Trang", true, "Blue", 350, 37);
            CreateTile("Thuế lương bổng", false, "White", 0, 38);
            CreateTile("Cố Đô Huế", true, "Blue", 400, 39);

            Players[0] = new Player();
            Players[1] = new Player();
            #endregion //Cập nhật giao diện người chơi 
        }

        private void Game_Load(object sender, EventArgs e)
        {
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
                        Stream.Write(
                            Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " đã rời"),
                            0,
                            Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " đã rời").Length);
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
                        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        serverSocket.Connect(ConnectionOptions.IP, ConnectionOptions.Port);
                        Stream = new NetworkStream(serverSocket);
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

                    SendMessageToServer("Người chơi mới đã vào");


                    //Hiển thị Form chọn màu 
                    ColorChoosing colorChoosing = new ColorChoosing();
                    colorChoosing.ShowDialog();
                    //Nếu chọn Cancel thì hủy kết nối ròi quay về MainMenu chính 
                    if (colorChoosing.DialogResult is DialogResult.Cancel)
                    {
                        MainMenu mainMenu = new MainMenu();
                        mainMenu.ShowDialog();
                        SendMessageToServer(ConnectionOptions.PlayerName + " đã rời");
                        Disconnect();
                    }
                    //Gửi tên  người chơi đến server
                    SendMessageToServer(ConnectionOptions.PlayerName);

                    //Xác định người chơi hiện tại và đánh dấu họ đã kết nối 
                    if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Đỏ\s*\(\s*(\d+)\s*\)"))
                    {
                        colorLb.BackColor = Color.Red;
                        RedConnected = true;
                        CurrentPlayerId = 0;
                    }
                    else if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Xanh\s*\(\s*(\d+)\s*\)"))
                    {
                        colorLb.BackColor = Color.Blue;
                        BlueConnected = true;
                        CurrentPlayerId = 1;
                    }
                    colorLb.Text = ConnectionOptions.Room;
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            
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
        private void InJail()
        {
            //Tăng số lần vào tù 
            Players[CurrentPlayerId].Jail += 1;
            //Vô hiệu hóa các nút khi vào tù 
            buyBtn.Enabled = false;
            throwDiceBtn.Enabled = false;
            endTurnBtn.Enabled = true;
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
            if (Players[CurrentPlayerId].Jail != 3) 
                return;
            //Sau khi thả người chơi thì hiển thị thông báo trên dao diện
            Players[CurrentPlayerId].InJail = false;
            Players[CurrentPlayerId].Jail = 0;
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
                    // Tách chuỗi ra để dễ cho việc xử lý từng phòng chơi
                    string[] parts = message.Split(new char[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

                    //Nhận được thông điệp máy chủ cả 2 ngươi chơi đều đã kết nối
                    //if (Regex.IsMatch(message, @"Cả\s+2\s+người\s+chơi\s+đã\s+kết\s+nối:\s+\d+") && parts[parts.Length - 1] == ConnectionOptions.Room)
                    if (message.Contains("Cả 2 người chơi đã kết nối: ") && parts[parts.Length - 1] == ConnectionOptions.Room)
                    {
                        if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Đỏ\s*\(\s*(\d+)\s*\)")) 
                            currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                            {
                                currentPlayersTurn_textbox.Text = "Tung xúc sắc để bắt đầu trò chơi";
                                throwDiceBtn.Enabled = true;
                                buyBtn.Enabled = false;
                                endTurnBtn.Enabled = false;
                            });
                        if (Regex.IsMatch(ConnectionOptions.PlayerName, @"Xanh\s*\(\s*(\d+)\s*\)"))
                            currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                            {
                                currentPlayersTurn_textbox.Text = "Đỏ đang thực hiện lượt chơi. Chờ...";
                            });
                    }

                    //Khi người chơi màu đỏ đã kết nối 
                    else if (Regex.IsMatch(message, @"Đỏ\s*\(\s*(\d+)\s*\)\s*đã kết nối") && parts[1] == ConnectionOptions.Room)
                    {
                        RedConnected = true;
                        // Kiểm tra xem người chơi màu xanh có kết nối không và gửi thông báo nếu cả hai đã kết nối
                        if (!BlueConnected) 
                            continue;
                        SendMessageToServer("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room);
                    }

                    //Khi người chơi màu xanh đã kết nối 
                    else if (Regex.IsMatch(message, @"Xanh\s*\(\s*(\d+)\s*\)\s*đã kết nối") && parts[1] == ConnectionOptions.Room)
                    {
                        BlueConnected = true;
                        // Kiểm tra xem người chơi màu đỏ có kết nối không và gửi thông báo nếu cả hai đã kết nối
                        if (!RedConnected) 
                            continue;
                        SendMessageToServer("Cả 2 người chơi đã kết nối: " + ConnectionOptions.Room);

                    }

                    //Xử lý tin nhắn
                    if (message.Contains(" nhắn: "))
                    {

                        this.Invoke(new MethodInvoker(delegate
                        {
                            if (parts[1] == ConnectionOptions.Room)
                            {
                                string message_show = message;
                                message_show = message_show.Replace(" nhắn", "");
                                messageRTB.Invoke((MethodInvoker)delegate
                                {
                                    messageRTB.AppendText(message_show + Environment.NewLine);
                                });
                            }
                        }));
                    }

                    if (message.Contains(" đã rời") && parts[1] == ConnectionOptions.Room)
                    {
                        SendMessageToServer(ConnectionOptions.PlayerName + " đã rời.");
                        this.Invoke((MethodInvoker)delegate {
                            MessageBox.Show("Đối thủ của bạn đã rời", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                            this.Hide();
                            MainMenu mainMenu = new MainMenu();
                            mainMenu.ShowDialog();
                            Disconnect();
                        });
                    }

                    //Khi nhận được kết quả lượt đi 
                    //Xử lý thông tin nhận được và cập nhật kết quả cho người 
                    if (message.Contains("Kết quả lượt đi") && parts[0] == ConnectionOptions.Room)
                    {
                        // Lưu tin nhắn gốc
                        var tempMessage = message;
                        var subString = string.Empty;

                        // Xác định xem lượt đi này thuộc về người chơi nào
                        switch (CurrentPlayerId)
                        {
                            case 0:
                                subString = "Kết quả lượt đi của Xanh";
                                break;
                            case 1:
                                subString = "Kết quả lượt đi của Đỏ";
                                break;
                        }
                        // Loại bỏ chuỗi xác định lượt đi của một người chơi khỏi tin nhắn
                        tempMessage = tempMessage.Replace(subString, "");

                        // Thực hiện các thay đổi giao diện người dùng bằng cách sử dụng Invoke để đảm bảo chúng chạy trên luồng chính
                        currentPlayersTurn_textbox.Invoke((MethodInvoker)delegate
                        {
                            // Cập nhật trạng thái của textbox hiển thị lượt của người chơi hiện tại
                            currentPlayersTurn_textbox.Text = "Lượt của bạn";
                            // Kích hoạt nút để ném xúc xắc
                            throwDiceBtn.Enabled = true;
                            // Vô hiệu hóa nút mua đất
                            buyBtn.Enabled = false;
                            // Vô hiệu hóa nút kết thúc lượt đi
                            endTurnBtn.Enabled = false;
                        });

                        // Tạo một đối tượng ReceivedMessage để lưu trữ thông điệp nhận được
                        ReceivedMessage receivedMessage = new ReceivedMessage();

                        // Lấy vị trí kết thúc lượt đi từ tin nhắn
                        String stringPosition = tempMessage.Split('~')[1];
                        receivedMessage.EndPosition = Convert.ToInt32(stringPosition);

                        // Lấy số tiền sau lượt đi từ tin nhắn
                        String stringBalance = tempMessage.Split('~')[2];
                        receivedMessage.Balance = Convert.ToInt32(stringBalance);

                        // Lấy tài sản (đất) hiện có từ tin nhắn
                        String stringPropertiesOwned = tempMessage.Split('~')[3];
                        if (stringPropertiesOwned != "NULL")
                        {
                            // Lấy mã số của các nhà được sở hữu
                            int[] tempArrayOfPropertiesOwned = stringPropertiesOwned
                                .Split(' ')
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Select(x => int.Parse(x))
                                .ToArray();
                            for (int k = 0; k < tempArrayOfPropertiesOwned.Length; k++)
                                receivedMessage.PropertiesOwned[k] = tempArrayOfPropertiesOwned[k];
                        }

                        // Cập nhật trạng thái của người chơi
                        switch (CurrentPlayerId)
                        {
                            case 0:
                                // Đổi lượt điều khiển sang người chơi tiếp theo
                                CurrentPlayerId = 1;
                                // Di chuyển biểu tượng của người chơi đến vị trí kết thúc lượt đi
                                Invoke((MethodInvoker)delegate
                                {
                                    MoveIcon(receivedMessage.EndPosition);
                                });
                                // Cập nhật vị trí và số dư của người chơi
                                Players[CurrentPlayerId].Position = receivedMessage.EndPosition;
                                Players[CurrentPlayerId].Balance = receivedMessage.Balance;

                                // Cập nhật danh sách tài sản được sở hữu của người chơi
                                int i = 0;
                                foreach (var item in receivedMessage.PropertiesOwned)
                                {
                                    Players[CurrentPlayerId].PropertiesOwned[i] = item;
                                    i++;
                                }

                                // Vẽ các biểu tượng tài sản mà người chơi đang sở hữu
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
                                // Đổi lượt điều khiển trở lại người chơi ban đầu
                                CurrentPlayerId = 0;
                                // Cập nhật hộp thông tin trạng thái của các người chơi
                                UpdatePlayersStatusBoxes();
                                break;

                            case 1:
                                // Đổi lượt điều khiển sang người chơi tiếp theo
                                CurrentPlayerId = 0;
                                // Di chuyển biểu tượng của người chơi đến vị trí kết thúc lượt đi
                                Invoke((MethodInvoker)delegate
                                {
                                    MoveIcon(receivedMessage.EndPosition);
                                });
                                // Cập nhật vị trí và số dư của người chơi
                                Players[CurrentPlayerId].Position = receivedMessage.EndPosition;
                                Players[CurrentPlayerId].Balance = receivedMessage.Balance;

                                // Cập nhật danh sách tài sản được sở hữu của người chơi
                                int k = 0;
                                foreach (var item in receivedMessage.PropertiesOwned)
                                {
                                    Players[CurrentPlayerId].PropertiesOwned[k] = item;
                                    k++;
                                }

                                // Vẽ các biểu tượng tài sản mà người chơi đang sở hữu
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
                                // Đổi lượt điều khiển trở lại người chơi ban đầu
                                CurrentPlayerId = 1;
                                // Cập nhật hộp thông tin trạng thái của các người chơi
                                UpdatePlayersStatusBoxes();
                                break;
                        }

                        // Kiểm tra nếu số dư của người chơi trở thành âm
                        if (Convert.ToInt32(stringBalance) < 0)
                            Win(); // Gọi hàm Win để xử lý việc người chơi đã thua cuộc
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
                catch
                {
                    MessageBox.Show("Đã mất kết nối");
                    Disconnect();
                }
        }
        //Hàm được gọi khi người chơi thua cuộc
        private void Lose()
        {
            if (MessageBox.Show("Bạn đã thua! Chúc may mắn lần sau!", "Thông báo", MessageBoxButtons.OK) == DialogResult.OK)
            {
                SendMessageToServer(ConnectionOptions.PlayerName + " thua.");
                this.Hide();
                MainMenu mainMenu = new MainMenu();
                mainMenu.ShowDialog();
            }
        }

        private void Win()
        {
            if (MessageBox.Show("Bạn đã thắng! Congratulations!", "Thông báo", MessageBoxButtons.OK) == DialogResult.OK)
            {
                SendMessageToServer(ConnectionOptions.PlayerName + " thắng.");
                this.Hide();
                MainMenu mainMenu = new MainMenu();
                mainMenu.ShowDialog();
            }

        }
        //Phương thức ngắt kết nối và thoát ứng dụng 
        private static void Disconnect()
        {
            Stream?.Close();
            serverSocket?.Close();
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
                return;
            SendMessageToServer(" nhắn: " + message);
            messageTb.Text = "";
        }

        private void Game_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Gamemodes.Multiplayer)
                SendMessageToServer(ConnectionOptions.PlayerName + " đã rời");
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
            //Cho phép người chơi mua đất
            buyBtn.Enabled = true;
            endTurnBtn.Enabled = true;

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
                InJail();

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
                    InJail();
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
                        SendMessageToServer(rentMessage);
                    }
                    break;
                case 1:
                    ChangeBalance(Players[1], -GetRent(Dice));
                    ChangeBalance(Players[0], GetRent(Dice));
                    if (Gamemodes.Multiplayer)
                    {
                        string rentMessage = ConnectionOptions.Room + " Trả tiền thuê nhà cho Đỏ: : " + GetRent(Dice);
                        MessageBox.Show("Xanh trả tiền thuê nhà cho Đỏ: : " + GetRent(Dice));
                        SendMessageToServer(rentMessage);
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
        }

        private void QuitGameBtn_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có muốn thoát", "Thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (Gamemodes.Multiplayer)
                    SendMessageToServer(ConnectionOptions.PlayerName + " đã rời");

                this.Hide();
                MainMenu mainMenu = new MainMenu();
                mainMenu.ShowDialog();
            }
        }

        private void EndTurnBtn_Click(object sender, EventArgs e)
        {
            if (Gamemodes.Multiplayer)
            {
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
                    + Players[CurrentPlayerId].Balance + '~';
                foreach (var item in Players[CurrentPlayerId].PropertiesOwned)
                    if (item != 0)
                    {
                        turnLogString += item;
                        turnLogString += ' ';
                    }
                if (CurrentPlayerId is 0)
                {
                    currentPlayersTurn_textbox.Text = "Xanh đang thực hiện lượt chơi. Chờ...";
                    SendMessageToServer(turnLogString);
                }
                else {
                    currentPlayersTurn_textbox.Text = "Đỏ đang thực hiện lượt chơi. Chờ...";
                    SendMessageToServer(turnLogString);
                }
                SendMessageToServer(turnLogString);


                if (Players[CurrentPlayerId].Balance < 0)
                    Lose();
                else
                {
                    throwDiceBtn.Enabled = false;
                    buyBtn.Enabled = false;
                    endTurnBtn.Enabled = false;
                }
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
                }
                if (Players[CurrentPlayerId].Balance < 0)
                    Lose();
                else
                {
                    throwDiceBtn.Enabled = false;
                    buyBtn.Enabled = false;
                    endTurnBtn.Enabled = false;
                }
                currentPositionInfo_richtextbox.Text = string.Empty;
            }
        }
        private static void SendMessageToServer(string message)
        {
            try
            {
                // chuyển chuỗi thành các byte chuyển đi
                byte[] data = Encoding.Unicode.GetBytes(message);

                // Gửi chuỗi thông qua stream
                Stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                // hiển thị màn hình là bị lỗi
                MessageBox.Show("Lỗi khi gửi thông báo tới Server: " + ex.Message);
            }
        }
    }
}