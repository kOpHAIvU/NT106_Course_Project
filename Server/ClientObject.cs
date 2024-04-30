using System;
using System.Net.Sockets;
using System.Text;
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
                    switch (message)
                    {
                        case "Cả 2 người chơi đã kết nối":
                            {
                                server.SendMessageToEveryone(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                break;
                            }
                        case "Đỏ":
                            {
                                Taken.Red = true;
                                userName = message;
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                                });
                                server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                                break;
                            }
                        case "Xanh":
                            {
                                Taken.Blue = true;
                                userName = message;
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + userName + " đã kết nối" + Environment.NewLine;
                                });
                                server.SendMessageToOpponentClient(userName + " đã kết nối", Id);
                                break;
                            }
                        case "Người chơi mới đã vào":
                            {
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                if (Taken.Red) 
                                    server.SendMessageToSender("Quân tốt Đỏ đã được chọn", Id);
                                if (Taken.Blue) 
                                    server.SendMessageToSender("Quân tốt Xanh đã được chọn", Id);
                                break;
                            }
                        case "Quân tốt Đỏ đã được chọn":
                            {
                                server.SendMessageToOpponentClient(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                break;
                            }
                        case "Quân tốt Xanh đã được chọn":
                            {
                                server.SendMessageToOpponentClient(message, Id);
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                break;
                            }
                        case "Đỏ đã rời" when userName is "Đỏ":
                            {
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                server.RemoveConnection(this.Id);
                                break;
                            }
                        case "Xanh đã rời" when userName is "Xanh":
                            {
                                Program.f.tbLog.Invoke((MethodInvoker)delegate
                                {
                                    Program.f.tbLog.Text += "[" + DateTime.Now + "] " + message + Environment.NewLine;
                                });
                                server.RemoveConnection(this.Id);
                                break;
                            }
                    }

                    if (message.Contains("nhắn : "))
                    {
                        server.SendMessageToEveryone( userName + " " + message,Id);
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



