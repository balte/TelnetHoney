using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TeletHoney.src
{
    public class Server
    {
        private Socket Handle;
        public Peer[] Peers { get; private set; }
        private List<Peer> PeerList;
        public string ConnectMessage { get; private set; }

        /// <summary>
        /// Start a new instance of the server
        /// </summary>
        /// <param name="ListenIp">The IP to listen at, default: 0.0.0.0</param>
        /// <param name="ConnectMessage">The message to send to the client (peer) once connected</param>
        public Server(string ListenIp, string ConnectMessage)
        {
            this.PeerList = new List<Peer>();
            this.ConnectMessage = ConnectMessage;
            this.Handle = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Handle.Bind(new IPEndPoint(IPAddress.Parse(ListenIp), 23));
            this.Handle.Listen(100);
            this.Handle.BeginAccept(AcceptClient, null);
        }

        private void AcceptClient(IAsyncResult ar)
        {
            try
            {
                Socket PeerSocket = this.Handle.EndAccept(ar);
                PeerList.Add(new Peer(PeerSocket, this));
            }
            catch
            {
                
            }

            this.Handle.BeginAccept(AcceptClient, null);
        }
    }
}