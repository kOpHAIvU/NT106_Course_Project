using System;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
                    //Xác định người chơi hiện tại và đánh dấu họ đã kết nối 
                    switch (ConnectionOptions.PlayerName)
                    {
                        case "Red":
                            RedConnected = true;
                            CurrentPlayerId = 0;
                            break;
                        case "Blue":
                            BlueConnected = true;
                            CurrentPlayerId = 1;
                            break;
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
            CreateTile("Khí vận", false, "Opportunity ", 0, 2);
            CreateTile("Nhà bè Phú Xuân", true, "Purple", 60, 3);
            CreateTile("Thuế lợi tức", false, "White", 0, 4);
            CreateTile("Bến xe Lục Tỉnh", true, "Station", 200, 5);
            CreateTile("Thị Nghè", true, "Turquoise", 100, 6);
            CreateTile("Cơ hội", false, "Opportunity ", 0, 7);
            CreateTile("Tân Định", true, "Turquoise", 100, 8);
            CreateTile("Bến Chương Dương", true, "Turquoise", 120, 9);
            CreateTile("Thăm tù", false, "Null", 0, 10);
            CreateTile("Phan Đình Phùng", true, "Pink", 140, 11);
            CreateTile("Công ty điện lực", false, "White", 0, 12);
            CreateTile("Trịnh Minh Thế", true, "Pink", 140, 13);
            CreateTile("Lý Thái Tổ", true, "Pink", 160, 14);
            CreateTile("Bến xe Lam Chợ Lớn", true, "Station", 200, 15);
            CreateTile("Đại lộ Hùng Vương", true, "Orange", 180, 16);
            CreateTile("Khí vận", false, "Opportunity ", 0, 17);
            CreateTile("Gia Long", true, "Orange", 180, 18);
            CreateTile("Bến Bạch Đằng", true, "Orange", 200, 19);
            CreateTile("Sân bay", false, "Null", 0, 20);
            CreateTile("Đường Công Lý", true, "Red", 220, 21);
            CreateTile("Cơ hội", false, "Opportunity ", 0, 22);
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
            CreateTile("Khí vận", false, "Opportunity ", 0, 33);
            CreateTile("Nguyễn Thái Học", true, "Green", 320, 34);
            CreateTile("Tân Kì Tân Quý", false, "White", 0, 35);
            CreateTile("Cơ hội", false, "Opportunity ", 0, 36);
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

        //Hàm thay đổi trạng thái các ô
        private void CreateTile(string tileName, bool tileBuyable, string tileColor, int tilePrice, int tilePosition)
        {
            Property property = new Property
            {
                Name = tileName,
                Color = tileColor,
                Buyable = tileBuyable,
                Price = tilePrice
            };
            Properties[tilePosition] = property;
        }

        private string PropertiesToString(int[] propertyList)
        {
            var tempString = "";
            for (var i = 0; i < 40; i++)
                if (propertyList[i] != 0)
                    tempString = tempString + Properties[propertyList[i]].Name + ", " + Properties[propertyList[i]].Color + "\n";
            return tempString;
        }

        private void UpdatePlayersStatusBoxes()
        {
            redPlayerStatusBox_richtextbox.Text = "Red player" + "\n"
                + "Tiền còn lại: " + Players[0].Balance + "\n"
                + PropertiesToString(Players[0].PropertiesOwned);
            bluePlayerStatusBox_richtextbox.Text = "Blue player" + "\n"
                + "Tiền còn lại: " + Players[1].Balance + "\n"
                + PropertiesToString(Players[1].PropertiesOwned);
        }

        private void ChangeBalance(Player player, int cashChange)
        {
            player.Balance += cashChange;
            UpdatePlayersStatusBoxes();
        }

        private void InJail(int currentPlayer)
        {
            Players[currentPlayer].Jail += 1;
            buyBtn.Enabled = false;
            throwDiceBtn.Enabled = false;
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text =
                        "Người chơi màu đỏ, bạn đang ở trong tù!\r\nSẽ được di chuyển ở lượt tiếp. "; break;
                case 1:
                    currentPlayersTurn_textbox.Text =
                        "Người chơi màu xanh, bạn đang ở trong tù!\r\nSẽ được di chuyển ở lượt tiếp. ";
                    break;
            }
            if (Players[currentPlayer].Jail != 3) return;
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

        private int GetRent(int dice)
        {
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

        private void DrawCircle(int position, int playerId)
        {
            int x = Tile[position].Location.X, y = Tile[position].Location.Y;
            switch (playerId)
            {
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
                        Controls.Add(redMarker);
                        redMarker.BringToFront();
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
                        BlueDotsNameSplitter++;
                        break;
                    }
            }
        }

        private void ReceiveMessage()
        {
            while (true)
                try
                {
                    byte[] data = new byte[256];
                    StringBuilder builder = new StringBuilder();
                    do
                    {
                        var bytes = Stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    } while (Stream.DataAvailable);

                    String message = builder.ToString();
                    switch (message)
                    {
                        case "Cả 2 người chơi đã kết nối":
                            {
                                switch (ConnectionOptions.PlayerName)
                                {
                                    case "Red":
                                        currentPlayersTurn_textbox.Text = "Tung xúc sắc để bất đầu trò chơi";
                                        throwDiceBtn.Enabled = true;
                                        buyBtn.Enabled = false;
                                        endTurnBtn.Enabled = true;
                                        break;
                                    case "Blue":
                                        currentPlayersTurn_textbox.Text = "Đỏ đang thực hiện lượt chơi. Chờ...";
                                        break;
                                }
                                break;
                            }
                        case "Đỏ đã kết nối":
                            {
                                RedConnected = true;
                                ConnectionOptions.NameRedIsTaken = true;
                                if (!BlueConnected) continue;
                                Stream.Write(Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối"), 0, Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối").Length);
                                break;
                            }
                        case "Xanh đã kết nối":
                            {
                                BlueConnected = true;
                                ConnectionOptions.NameBlueIsTaken = true;
                                if (!RedConnected) continue;
                                Stream.Write(Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối"), 0, Encoding.Unicode.GetBytes("Cả 2 người chơi đã kết nối").Length);
                                break;
                            }
                        case "Quân tốt Đỏ đã được chọn":
                            {
                                ConnectionOptions.NameRedIsTaken = true;
                                break;
                            }
                        case "Quân tốt Xanh đã được chọn":
                            {
                                ConnectionOptions.NameBlueIsTaken = true;
                                break;
                            }
                    }

                    if (message.Contains("Kết quả lượt đi"))
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

                        if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance < 0) 
                            Lose();
                        int count = 0;
                        for (var u = 0; u < 2; u++)
                        {
                            if (Players[u].Loser) count++;
                            if (Players[CurrentPlayerId].Loser || count < 1) continue;
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
                        switch (CurrentPlayerId)
                        {
                            case 0:
                                CurrentPlayerId = 1;
                                MoveIcon(receivedMessage.EndPosition);
                                Players[CurrentPlayerId].Position = receivedMessage.EndPosition;
                                Players[CurrentPlayerId].Balance = receivedMessage.Balance;
                                Players[CurrentPlayerId].InJail = receivedMessage.InJail;
                                Players[CurrentPlayerId].Jail = receivedMessage.Jail;
                                if (Players[CurrentPlayerId].InJail) InJail(CurrentPlayerId);
                                var i = 0;
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
                                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) Lose();
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
                                if (Players[CurrentPlayerId].InJail) InJail(CurrentPlayerId);
                                var k = 0;
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
                                if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) Lose();
                                CurrentPlayerId = 1;
                                UpdatePlayersStatusBoxes();
                                break;
                        }
                    }
                    if (message.Contains("Rent to the Red player"))
                    {
                        var sumOfRentString = message.Replace("Rent to the Red player: ", "");
                        var sumOfRent = Convert.ToInt32(sumOfRentString);
                        ChangeBalance(Players[1], -sumOfRent);
                        ChangeBalance(Players[0], sumOfRent);
                        MessageBox.Show("Blue player payed rent to the Red player: " + sumOfRent);
                    }
                    else if (message.Contains("Rent to the Blue player"))
                    {
                        var sumOfRentString = message.Replace("Rent to the Blue player: ", "");
                        var sumOfRent = Convert.ToInt32(sumOfRentString);
                        ChangeBalance(Players[0], -sumOfRent);
                        ChangeBalance(Players[1], sumOfRent);
                        MessageBox.Show("Red player payed rent to the Blue player: " + sumOfRent);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection has been lost! " + ex.Message);
                    Disconnect();
                }
        }
        private void Lose()
        {
            Players[CurrentPlayerId].Loser = true;
            throwDiceBtn.Enabled = false;
            buyBtn.Enabled = false;
            endTurnBtn.Enabled = false;
            switch (CurrentPlayerId)
            {
                case 0 when Players[0].Loser:
                    currentPlayersTurn_textbox.Text = "Red player has lost!";
                    break;
                case 1 when Players[1].Loser:
                    currentPlayersTurn_textbox.Text = "Blue player has lost!";
                    break;
            }
        }
        private static void Disconnect()
        {
            Stream?.Close();
            Client?.Close();
            Environment.Exit(0);
        }
        private void MoveIcon(int position)
        {
            int x, y;
            switch (CurrentPlayerId)
            {
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

        private async Task<int> MoveTileByTile(int from, int to)
        {
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
        private void ThrowDiceBtn_Click(object sender, EventArgs e)
        {
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text = "Red player's turn. ";
                    break;
                case 1:
                    currentPlayersTurn_textbox.Text = "Blue player's turn. ";
                    break;
            }
            bool visitedJailExploration = false, visitedTaxTile = false, visitedGo = false, visitedFreeParking = false, goingToJail = false;
            buyBtn.Enabled = true;
            UpdatePlayersStatusBoxes();
            var rand = new Random();
            var firstDice = rand.Next(1, 7);
            var secondDice = rand.Next(1, 7);
            Dice = firstDice + secondDice;
            whatIsOnDices_textbox.Text = "On dices: " + firstDice + " and " + secondDice + ". Summary: " + Dice + ". ";
            throwDiceBtn.Enabled = false;
            var positionBeforeDicing = Players[CurrentPlayerId].Position;
            CurrentPosition = Players[CurrentPlayerId].Position + Dice;
            var positionAfterDicing = Players[CurrentPlayerId].Position + Dice;
            if (Players[CurrentPlayerId].InJail) InJail(CurrentPlayerId);
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
            {
                ChangeBalance(Players[CurrentPlayerId], -200);
                buyBtn.Enabled = false;
                visitedTaxTile = true;
            }
            Players[CurrentPlayerId].Position = CurrentPosition;
            switch (goingToJail)
            {
                case true:
                    MoveIcon(10);
                    break;
                case false:
                    _ = MoveTileByTile(positionBeforeDicing, positionAfterDicing);
                    break;
            }
            currentPositionInfo_richtextbox.Text = "Position " + CurrentPosition;
            currentPositionInfo_richtextbox.AppendText("\r\n" + Properties[CurrentPosition].Name);
            currentPositionInfo_richtextbox.AppendText("\r\n" + "Price " + Properties[CurrentPosition].Price);
            currentPositionInfo_richtextbox.AppendText("\r\n" + "Type " + Properties[CurrentPosition].Color);
            if (visitedJailExploration) currentPositionInfo_richtextbox.AppendText("\r\n" + "You visited the jail with a tour. ");
            if (visitedTaxTile) currentPositionInfo_richtextbox.AppendText("\r\n" + "You paid tax!");
            if (visitedGo) currentPositionInfo_richtextbox.AppendText("\r\n" + "Take +200 for passing \"GO\" tile. ");
            if (visitedFreeParking) currentPositionInfo_richtextbox.AppendText("\r\n" + "Chill...");
            if (goingToJail)
            {
                currentPositionInfo_richtextbox.AppendText("\r\n" + "You are in jail");
                switch (CurrentPlayerId)
                {
                    case 0:
                        currentPlayersTurn_textbox.Text = "Red player, you are in jail! \r\nYou will skip this and next turn. ";
                        break;
                    case 1:
                        currentPlayersTurn_textbox.Text = "Blue player, you are in jail! \r\nYou will skip this and next turn. ";
                        break;
                }
            }
            currentPositionInfo_richtextbox.ScrollToCaret();
            if (Players[CurrentPlayerId].Loser || Players[CurrentPlayerId].Balance <= 0) Lose();
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                if (Players[i].Loser) count++;
                if (Players[CurrentPlayerId].Loser || count < 1) continue;
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
            if (Players[CurrentPlayerId].PropertiesOwned[CurrentPosition] == CurrentPosition || !Properties[CurrentPosition].Owned) return;
            buyBtn.Enabled = false;
            switch (CurrentPlayerId)
            {
                case 0:
                    ChangeBalance(Players[0], -GetRent(Dice));
                    ChangeBalance(Players[1], GetRent(Dice));
                    if (Gamemodes.Multiplayer)
                    {
                        var rentMessage = "Rent to the Blue player: " + GetRent(Dice);
                        MessageBox.Show("Red player payed rent to the Blue player: " + GetRent(Dice));
                        Stream.Write(Encoding.Unicode.GetBytes(rentMessage), 0, Encoding.Unicode.GetBytes(rentMessage).Length);
                    }
                    break;
                case 1:
                    ChangeBalance(Players[1], -GetRent(Dice));
                    ChangeBalance(Players[0], GetRent(Dice));
                    if (Gamemodes.Multiplayer)
                    {
                        var rentMessage = "Rent to the Red player: " + GetRent(Dice);
                        MessageBox.Show("Blue player payed rent to the Red player: " + GetRent(Dice));
                        Stream.Write(Encoding.Unicode.GetBytes(rentMessage), 0, Encoding.Unicode.GetBytes(rentMessage).Length);
                    }
                    break;
            }
            switch (CurrentPlayerId)
            {
                case 0:
                    currentPlayersTurn_textbox.Text = "Red player, you landed on another player's tile and payed ";
                    break;
                case 1:
                    currentPlayersTurn_textbox.Text = "Blue player, you landed on another player's tile and payed ";
                    break;
            }
            if (CurrentPosition is 5 || CurrentPosition is 15 || CurrentPosition is 25 || CurrentPosition is 35) currentPlayersTurn_textbox.Text += Dice * 20;
            else currentPlayersTurn_textbox.Text += Properties[CurrentPosition].Rent;
        }
        private void BuyBtn_Click(object sender, EventArgs e)
        {
            if (Properties[CurrentPosition].Buyable && Properties[CurrentPosition].Owned is false)
                if (Players[CurrentPlayerId].Balance >= Properties[CurrentPosition].Price)
                {
                    ChangeBalance(Players[CurrentPlayerId], -Properties[CurrentPosition].Price);
                    Players[CurrentPlayerId].PropertiesOwned[Players[CurrentPlayerId].NumberOfPropertiesOwned] = CurrentPosition;
                    Properties[CurrentPosition].Owned = true;
                    Players[CurrentPlayerId].NumberOfPropertiesOwned++;
                    UpdatePlayersStatusBoxes();
                    buyBtn.Enabled = false;
                    DrawCircle(CurrentPosition, CurrentPlayerId);
                }
                else currentPlayersTurn_textbox.Text = "You don't have enough money for that";
            else currentPlayersTurn_textbox.Text = "You cannot do that right now";
            if (Players[CurrentPlayerId].Balance <= 0) Lose();
        }
        private void QuitGameBtn_Click(object sender, EventArgs e)
        {
            var dialog = MessageBox.Show("Do you really want ot exit?", "Exit", MessageBoxButtons.YesNo);
            switch (dialog)
            {
                case DialogResult.Yes:
                    if (Gamemodes.Multiplayer) Stream.Write(
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " has left"),
                        0,
                        Encoding.Unicode.GetBytes(ConnectionOptions.PlayerName + " has left").Length);
                    Disconnect();
                    Application.Exit();
                    break;
                case DialogResult.No:
                    break;
            }
        }
        private void EndTurnBtn_Click(object sender, EventArgs e)
        {
            if (Gamemodes.Multiplayer)
            {
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
                var turnLogString = string.Empty;
                switch (CurrentPlayerId)
                {
                    case 0:
                        turnLogString = "Kết quả lượt đi của Đỏ";
                        break;
                    case 1:
                        turnLogString = "Kết quả lượt đi của Xanh";
                        break;
                }
                turnLogString += CurrentPlayerId.ToString() + '~'
                    + Players[CurrentPlayerId].Position + '~' +
                    Players[CurrentPlayerId].Balance + '~' +
                    Players[CurrentPlayerId].InJail + '~' +
                    Players[CurrentPlayerId].Jail + '~';
                foreach (var item in Players[CurrentPlayerId].PropertiesOwned)
                    if (item != 0)
                    {
                        turnLogString += item;
                        turnLogString += ' ';
                    }
                if (turnLogString.Last() is '~') turnLogString += "NULL";
                turnLogString += '~' + Players[CurrentPlayerId].Loser.ToString();
                if (CurrentPlayerId is 0)
                {
                    currentPlayersTurn_textbox.Text = "Blue player is making his turn right now, wait. ";
                    Stream.Write(Encoding.Unicode.GetBytes(turnLogString), 0, Encoding.Unicode.GetBytes(turnLogString).Length);
                }
                else
                {
                    currentPlayersTurn_textbox.Text = "Red player is making his turn right now, wait. ";
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
                        currentPlayersTurn_textbox.Text = "Red player's turn. ";
                        break;
                    case 1:
                        currentPlayersTurn_textbox.Text = "Blue player's turn. ";
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