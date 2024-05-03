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

                    // Nhận được thông điệp cả 2 người chơi đã kết nối
                    if (Regex.IsMatch(message, @"Cả\s+2\s+người\s+chơi\s+đã\s+kết\s+nối:\s+\d+"))
                    {
                        server.SendMessageToEveryone(message, Id);
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                        });
                    }

                    //Nhận được thông điệp đỏ đã kết nối
                    else if (Regex.IsMatch(message, @"Đỏ\s*\(\s*(\d+)\s*\)"))
                    {
                        Taken.Red = true;
                        userName = message;
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                        });
                        server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                    }

                    //Nhân được thông điệp xanh đã kết nối
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

                    //Nhận được thông điệp người chơi mới đã vào
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

                    //Nhận được thông điệp Quân tốt đỏ được chọn
                    else if (message == "Quân tốt Đỏ đã được chọn")
                    {
                        server.SendMessageToOpponentClient(message, Id);
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                        });
                    }

                    //Nhận được thông điệp quân tốt xanh được chọn
                    else if (message == "Quân tốt Xanh đã được chọn")
                    {
                        server.SendMessageToOpponentClient(message, Id);
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                        });
                    }

                    //Nhận được thông điệp rời đi của client
                    if (message.Contains(" đã rời"))
                    {
                        // Tìm vị trí của dấu cách đầu tiên
                        int vi_tri = message.IndexOf(' ');

                        // Lấy chuỗi từ đầu đến vị trí dấu cách đầu tiên
                        string chuoi_nho = message.Substring(0, vi_tri);

                        if(chuoi_nho == this.userName)
                        {
                            Program.f.tbLog.Invoke((MethodInvoker)delegate
                            {
                                Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                            });
                            server.RemoveConnection(this.Id);
                            break;
                        }
                    }
                       
                    //Nhận được tin nhắn của client và procast đến tất cả client còn lại
                    if (message.Contains(" nhắn: "))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName +  message + Environment.NewLine;
                        });
                        server.SendMessageToEveryone(userName + message, Id);
                    }


                    //Nhận được thông điệp kết thúc lượt và các thông số lượt đi trước của đỏ
                    if (message.Contains("Kết quả lượt đi của Đỏ"))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi" + Environment.NewLine;
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Xanh" + Environment.NewLine;
                        });
                        server.SendMessageToOpponentClient(message, Id);
                    }

                    //Nhận được thông điệp kết thúc lượt và các thông số lượt đi trước của xanh
                    if (message.Contains("Kết quả lượt đi của Xanh"))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi" + Environment.NewLine;
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Đỏ" + Environment.NewLine;
                        });
                        server.SendMessageToOpponentClient(message, Id);
                    }

                    //Nhận được thông điệp người chơi chịu mức thuế từ đối phương và số tiền thuế
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



