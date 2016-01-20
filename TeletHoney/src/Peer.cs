using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Random rnd;
        private Stopwatch TimeConnectedSW;

        //Settings
        private bool IsSudo = false;
        private string CurRootUser = "root@nsa.gov";

        //temp info for commands

        //W
        private int W_LoggedInUsers;

        private DateTime SystemDate;

        public Peer(Socket PeerHandle, Server server)
        {
            this.PeerHandle = PeerHandle;
            this.server = server;
            this.Buffer = new byte[128];
            this.rnd = new Random();

            this.SystemDate = DateTime.Now.Subtract(new TimeSpan(rnd.Next(-12, 12), rnd.Next(-5, 5), rnd.Next(-60, 60)));
            this.TimeConnectedSW = Stopwatch.StartNew();

            this.W_LoggedInUsers = 1; // rnd.Next(1, 10);

            try
            {
                this.LogStream = new FileStream("./" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss") + "__" + PeerHandle.RemoteEndPoint.ToString().Split(':')[0].Replace(":", "_").Replace(".", "_") + ".log", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                LogToFile("IP Address: " + PeerHandle.RemoteEndPoint.ToString().Split(':')[0]);
                LogToFile("Time: " + DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss"));
                LogToFile("");

                if (!String.IsNullOrEmpty(server.ConnectMessage))
                {
                    SendMessage(server.ConnectMessage + "\r\n");
                }

                WriteCurPath();

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

        private void WriteCurPath()
        {
            SendMessage(CurRootUser + ":/" + (IsSudo ? "#" : "$") + " ");
        }

        private DateTime GetSystemTime()
        {
            return SystemDate.Add(TimeConnectedSW.Elapsed);
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

                if(Buffer[0] == 0x08) //backspace
                {
                    this.PeerHandle.Send(ASCIIEncoding.UTF8.GetBytes("\u0020\u0008"));

                    if (CurrentCommand.Length > 0)
                    {
                        CurrentCommand = CurrentCommand.Substring(0, CurrentCommand.Length - 1);
                    }
                }
                else
                {
                    string Message = ASCIIEncoding.UTF8.GetString(Buffer, 0, Received);
                    CurrentCommand += Message;

                    if (CurrentCommand[CurrentCommand.Length - 1] == '\r' || CurrentCommand[CurrentCommand.Length - 1] == '\n')
                    {
                        LogToFile("[<-]" + CurrentCommand);

                        //execute command
                        string cmd = CurrentCommand.Substring(0, CurrentCommand.Length - 2);

                        switch(cmd)
                        {
                            case "cd":
                            {
                                SendMessage(@"root Access Denied, Access log updated");
                                break;
                            }
                            case "passwd":
                            {
                                SendMessage(@"root Access Denied, Access log updated");
                                break;
                            }
                            case "reboot":
                            case "shutdown":
                            {
                                SendMessage("Broadcast message from " + CurRootUser + "\r\n");
                                SendMessage("(\\dev\\jffs3)\r\n");
                                SendMessage("The system is going down in 5 seconds(s)!\r\n");
                                for (int i = 5; i > 0; i--)
                                {
                                    SendMessage(@"Rebooting in " + i + "\r\n");
                                    Thread.Sleep(1000);
                                }
                                SendMessage("Standby...\r\n");
                                Thread.Sleep(5000);
                                this.PeerHandle.Shutdown(SocketShutdown.Both);
                                this.PeerHandle.Close();
                                break;
                            }
                            case "w":
                            {
                                DateTime SysTime = GetSystemTime();
                                SendMessage("\r\n" + SysTime.ToString("HH:mm:ss") + " up " + SysTime.Day + " days, " + this.W_LoggedInUsers + " users, load average: 1." + rnd.Next(1, 100) + ", 0." + rnd.Next(1, 100) + ", 0." + rnd.Next(1, 100) + "\r\n" +
                                            "USER     TTY      FROM             LOGIN@   IDLE   JCPU   PCPU WHAT \r\n" +
                              String.Format("root     :0       :0               {0}    {1}.00s {2}.00s {3}.00s w", SysTime.ToString("HH:mm"), rnd.Next(0, 60), rnd.Next(0, 60), rnd.Next(0, 60)));
                                break;
                            }
                            case "sudo":
                            {
                                IsSudo = true;
                                break;
                            }
                            case "date":
                            {
                                DateTime SysTime = GetSystemTime();
                                string Day = SysTime.ToString("ddddd").Substring(0, 3);
                                string Month = SysTime.ToString("MMMMM").Substring(0, 3);
                                string DayNum = SysTime.ToString("dd");
                                string Time = SysTime.ToString("HH:mm:ss");

                                SendMessage(String.Format("{0} {1} {2} {3} CET {4}", Day, Month, DayNum, Time, SysTime.Year) + "\r\n");
                                break;
                            }
                            default:
                            {
                                SendMessage("-sh: " + cmd + ": not found");
                                break;
                            }
                        }

                        SendMessage("\r\n");
                        WriteCurPath();
                        CurrentCommand = "";
                    }
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
                this.PeerHandle.Send(ASCIIEncoding.UTF8.GetBytes(Message));
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