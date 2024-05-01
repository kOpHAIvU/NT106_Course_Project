using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Server
{
    public class ClientObject
    {
        private readonly TcpClient Client;
        private readonly ServerObject server;
        private string userName;
        private Dictionary<string, List<string>> playRooms = new Dictionary<string, List<string>>();
        private object locker = new object();
        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            Client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }
        protected internal string Id { get; }
        protected internal NetworkStream Stream { get; private set; }
        public void Process()
        {
            try
            {
                Stream = Client.GetStream();
                while (true)
                {
                    string message = GetMessage();
                        if (Regex.IsMatch(message, @"Cả\s+2\s+người\s+chơi\s+đã\s+kết\s+nối:\s+\d+"))
                            {
                                server.SendMessageToEveryone(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                            }
                        else if(Regex.IsMatch(message, @"Đỏ\s*\(\s*(\d+)\s*\)"))
                            {
                                Taken.Red = true;
                                userName = message;
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                                });
                                server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                            }
                        else if (Regex.IsMatch(message, @"Xanh\s*\(\s*(\d+)\s*\)"))
                            {
                                Taken.Blue = true;
                                userName = message;
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                                });
                                server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                            }
                        else if (message == "Người chơi mới đã vào")
                            {
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                if (Taken.Red)
                                    server.SendMessageToSender("Quân tốt Đỏ đã được chọn", Id);
                                if (Taken.Blue)
                                    server.SendMessageToSender("Quân tốt Xanh đã được chọn", Id);
                            }
                         else if (message == "Quân tốt Đỏ đã được chọn")
                            {
                                server.SendMessageToOpponentClient(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                            }
                        else if(message == "Quân tốt Xanh đã được chọn")
                            {
                                server.SendMessageToOpponentClient(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                            }
                        //case "Đỏ đã rời" when userName is "Đỏ":
                        //    {
                        //        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        //        {
                        //            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                        //        });
                        //        server.RemoveConnection(this.Id);
                        //        break;
                        //    }
                        //case "Xanh đã rời" when userName is "Xanh":
                        //    {
                        //        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        //        {
                        //            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                        //        });
                        //        server.RemoveConnection(this.Id);
                        //        break;
                        //    }

                    // Nếu thông điệp bắt đầu từ chữ join thì lập tức yêu cầu tạo phòng chơi cụ thể
                    if (message.StartsWith("/join"))
                    {
                        string[] parts = message.Split(' ');
                        if (parts.Length == 2)
                        {
                            string roomName = parts[1];
                            lock (locker)
                            {
                                //Kiểm tra phòng chat đã tồn tại chưa
                                if (!playRooms.ContainsKey(roomName))
                                {
                                    playRooms.Add(roomName, new List<string>());
                                }
                                playRooms[roomName].Add(userName);
                            }
                            //Gửi thông điệp vào phòng chat thành công
                            server.SendMessageToEveryone(userName + " nhắn : Đã tham gia vào phòng " + parts[1], Id);
                        }
                        else
                        {
                            server.SendMessageToEveryone("Invalid command. Usage: /join [roomName]", Id);
                        }
                    }
                    else
                    {
                        string[] parts = message.Split(' ');
                    }

                    // Check if client is in a specific room
                    bool isInRoom = false;
                    string room = null;
                    lock (locker)
                    {
                        foreach (var playRoom in playRooms)
                        {
                            // Nếu tên người chơi có trong danh sách thành viên của một phòng chơi
                            if (playRoom.Value.Contains(userName))
                            {
                                isInRoom = true;
                                room = playRoom.Key;
                                break;
                            }
                        }
                    }

                    // Nếu người chơi đang tham gia một phòng chơi cụ thể
                    if (isInRoom)
                    {
                        lock (locker)
                        {
                            foreach (var member in playRooms[room])
                            {


                                if (message.Contains("nhắn : "))
                                {
                                    server.SendMessageToEveryone(userName + " " + message, Id);
                                }

                                if (message.Contains("Kết quả lượt đi của Đỏ"))
                                {
                                    Program.f.tbLog.Invoke((MethodInvoker)delegate
                                    {
                                        Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đỏ đã hoàn thành lượt đi" + Environment.NewLine;
                                        Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Xanh" + Environment.NewLine;
                                    });
                                    server.SendMessageToOpponentClient(message, Id);
                                }
                                if (message.Contains("Kết quả lượt đi của Xanh"))
                                {
                                    Program.f.tbLog.Invoke((MethodInvoker)delegate
                                    {
                                        Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Xanh đã hoàn thành lượt đi" + Environment.NewLine;
                                        Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Đỏ" + Environment.NewLine;
                                    });
                                    server.SendMessageToOpponentClient(message, Id);
                                }
                                if (message.Contains("thuê"))
                                {
                                    Program.f.tbLog.Invoke((MethodInvoker)delegate
                                    {
                                        Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                    });
                                    server.SendMessageToOpponentClient(message, Id);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Program.f.tbLog.Invoke((MethodInvoker)delegate
                {
                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + e.Message + Environment.NewLine;
                });
            }
        }
        private string GetMessage()
        {
            byte[] data = new byte[256];
            StringBuilder builder = new StringBuilder();
            do
            {
                builder.Append(Encoding.Unicode.GetString(data, 0,
                    Stream.Read(data, 0, data.Length)));
            } while (Stream.DataAvailable);
            return builder.ToString();
        }
        protected internal void Close()
        {
            Stream.Close();
            Client.Close();
        }
    }
}



