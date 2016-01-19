using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TeletHoney.src
{
    public class Peer
    {
        public bool IsPortScanner { get; private set; }
        private Socket PeerHandle;
        private Server server;
        private byte[] Buffer;

        private ulong TotalReceived = 0;
        private string CurrentCommand = "";

        private FileStream LogStream;

        public Peer(Socket PeerHandle, Server server)
        {
            this.PeerHandle = PeerHandle;
            this.server = server;
            this.Buffer = new byte[128];

            try
            {
                this.LogStream = new FileStream("./" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + "__" + PeerHandle.RemoteEndPoint.ToString().Split(':')[0].Replace(":", "_").Replace(".", "_") + ".log", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                LogToFile("IP Address: " + PeerHandle.RemoteEndPoint.ToString().Split(':')[0]);
                LogToFile("Time: " + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss"));
                LogToFile("");

                

                if (!String.IsNullOrEmpty(server.ConnectMessage))
                {
                    SendMessage(server.ConnectMessage);
                    LogToFile("[->]" + server.ConnectMessage);
                }

                try
                {
                    this.PeerHandle.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch
                {
                    IsPortScanner = true;
                }
            }
            catch(Exception ex)
            {
                LogToFile("Peer disconnected in Peer Constructor, Possible PortScanner, " + ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int Received = this.PeerHandle.EndReceive(ar);

                if (Received <= 0)
                {
                    LogToFile("Peer disconnected unexpectedly");
                    LogToFile("Last Message in buffer: " + CurrentCommand);

                    try
                    {
                        LogStream.Dispose();
                    }
                    catch { }

                    return;
                }

                TotalReceived += (ulong)Received;

                string Message = ASCIIEncoding.UTF8.GetString(Buffer, 0, Received);
                CurrentCommand += Message;
                //LogToFile(Message);

                if (CurrentCommand[CurrentCommand.Length - 1] == '\r' || CurrentCommand[CurrentCommand.Length - 1] == '\n')
                {
                    LogToFile("[<-]" + CurrentCommand);

                    //execute command
                    string cmd = CurrentCommand.ToLower();

                    if (cmd.StartsWith("cd"))
                    {
                        SendMessage(@"root Access Denied, Access log updated");
                    }
                    else if (cmd.StartsWith("passwd"))
                    {
                        SendMessage(@"root Access Denied, Access log updated");
                    }
                    else if (cmd.StartsWith("w"))
                    {
                        SendMessage(@"root Access Denied, Access log updated");
                    }
                    else if (cmd.StartsWith("reboot") || cmd.StartsWith("shutdown"))
                    {
                        SendMessage("Broadcast message from root@nsa.gov.us");
                        SendMessage("(\\dev\\jffs3)");
                        SendMessage("The system is going down in 5 seconds(s)!");
                        for (int i = 5; i > 0; i--)
                        {
                            SendMessage(@"Rebooting in " + i);
                            Thread.Sleep(1000);
                        }
                        SendMessage("Standby...");
                        Thread.Sleep(5000);
                        this.PeerHandle.Shutdown(SocketShutdown.Both);
                        this.PeerHandle.Close();
                    }
                    else
                    {
                        SendMessage("Unknown command");
                    }
                    CurrentCommand = "";
                }

                this.PeerHandle.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch
            {
                LogToFile("Peer disconnected unexpectedly");
                LogToFile("Last Message in buffer: " + CurrentCommand);

                try
                {
                    LogStream.Dispose();
                }
                catch { }
            }

        }

        private void SendMessage(string Message)
        {
            try
            {
                LogToFile("[->]" + Message);
                this.PeerHandle.Send(ASCIIEncoding.UTF8.GetBytes(Message + "\r\n"));
            }
            catch { }
        }

        private void LogToFile(string Message)
        {
            try
            {
                byte[] temp = ASCIIEncoding.UTF8.GetBytes(Message + "\r\n");
                this.LogStream.Write(temp, 0, temp.Length);
                this.LogStream.Flush();

                Console.WriteLine(Message);
            }
            catch { }
        }
    }
}