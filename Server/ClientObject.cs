using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Server
{
    public class ClientObject
    {
        protected internal Socket Client;
        private readonly ServerObject server;
        private string userName;
        private StreamWriter write;
        DateTime now = DateTime.Now;
        public ClientObject(Socket socket, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            Client = socket;
            server = serverObject;
            serverObject.AddConnection(this);
        }
        protected internal string Id { get; }
        public void Process()
        {
            try
            {
                while (true)
                {
                    string message = GetMessage();

                    // Nhận được thông điệp cả 2 người chơi đã kết nối
                    //if (Regex.IsMatch(message, @"Cả\s+2\s+người\s+chơi\s+đã\s+kết\s+nối:\s+\d+"))
                    if (message.Contains("Cả 2 người chơi đã kết nối: "))
                    {
                        server.SendMessageToEveryone(message, Id);
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + message);
                        });
                    }
                    //Nhận được thông điệp đỏ đã kết nối
                    else if (Regex.IsMatch(message, @"Đỏ\s*\(\s*(\d+)\s*\)") 
                        && !message.Contains(" đã rời") 
                        && !message.Contains("thắng") 
                        && !message.Contains("thua"))
                    {
                        userName = message;
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + userName + " đã kết nối");
                        });
                        server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                    }
                    //Nhân được thông điệp xanh đã kết nối
                    else if (Regex.IsMatch(message, @"Xanh\s*\(\s*(\d+)\s*\)") 
                        && !message.Contains(" đã rời")
                        && !message.Contains("thắng")
                        && !message.Contains("thua"))
                    {
                        userName = message;
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + userName + " đã kết nối");
                        });
                        server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                    }
                    //Nhận được thông điệp rời đi của client
                    else if (message.Contains(" đã rời"))
                    {
                        if (!message.Contains(" đã rời."))
                            server.SendMessageToOpponentClient(message, Id);
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + message);
                        });
                        server.RemoveConnection(this.Id);
                        break;
                    }
                    //Nhận được tin nhắn của client và procast đến tất cả client còn lại
                    else if (message.Contains(" nhắn: "))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + message + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + userName + message);
                        });
                        server.SendMessageToEveryone(userName + message, Id);
                    }
                    //Nhận được thông điệp kết thúc lượt và các thông số lượt đi trước của đỏ
                    else if (message.Contains("Kết quả lượt đi của Đỏ"))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi");
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Xanh" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + "Đến lượt của Xanh");
                        });
                        server.SendMessageToOpponentClient(message, Id);
                    }
                    //Nhận được thông điệp kết thúc lượt và các thông số lượt đi trước của xanh
                    else if (message.Contains("Kết quả lượt đi của Xanh"))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + userName + " đã hoàn thành lượt đi");
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + "Đến lượt của Đỏ" + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + "Đến lượt của Đỏ");
                        });
                        server.SendMessageToOpponentClient(message, Id);
                    }
                    //Nhận được thông điệp người chơi chịu mức thuế từ đối phương và số tiền thuế
                    else if (message.Contains("thuê"))
                    {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + message);
                        });
                        server.SendMessageToOpponentClient(message, Id);
                    }
                    else if (message.Contains("thắng.") || message.Contains("thua.") || message == "Người chơi mới đã vào") {
                        Program.f.tbLog.Invoke((MethodInvoker)delegate
                        {
                            Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                            UpdateToFile("[" + DateTime.Now + "] " + message);
                        });
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
                int bytes = Client.Receive(data);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            } while (Client.Available > 0);

            return builder.ToString();
        }
        private void UpdateToFile(string data) 
        {
            if (write == null)
                write = new StreamWriter("Data\\" + userName + " " + $"Data_{now:yyyyMMdd_HHmmss}.txt");
            write.WriteLine(data);
            write.Flush();
        }
        protected internal void Close()
        {
            Client.Shutdown(SocketShutdown.Both);
            Client.Close();
        }
    }
}



