using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebSocketTCPRelay
{
    internal delegate void ReceiveBytes(byte[] data);

    class WebSocketServer
    {
        internal event ReceiveBytes OnReceiveBytes;

        private TcpListener listener;

        private List<WebSocketTcpClient> connectedWebSockets;

        public WebSocketServer(ushort listenPort)
        {
            connectedWebSockets = new List<WebSocketTcpClient>();
            listener = new TcpListener(IPAddress.Any, listenPort);
        }

        public void Start()
        {
            listener.Start();

            listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
        }

        public void Stop()
        {
            listener.Stop();
        }

        private void AcceptTcpClientCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;

            TcpClient client = listener.EndAcceptTcpClient(ar);

            Console.WriteLine("Client connected from " + client.Client.RemoteEndPoint);

            WebSocketTcpClient webSocketTcpClient = new WebSocketTcpClient(client);
            webSocketTcpClient.OnDidClose += (tcp, wsClient) =>
            {
                Console.WriteLine("Removing client ... " + tcp.Client.RemoteEndPoint);
                if (connectedWebSockets.Contains(webSocketTcpClient))
                {
                    connectedWebSockets.Remove(webSocketTcpClient);
                }
            };
            webSocketTcpClient.OnDidRead += (data) =>
            {
                OnReceiveBytes?.Invoke(data);
            };
            connectedWebSockets.Add(webSocketTcpClient);
            webSocketTcpClient.Start();

            // Wait for more clients
            listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
        }

        public void Write(byte[] data, bool asTextFrames = false)
        {
            for (int i = 0; i < connectedWebSockets.Count; i++)
            {
                WebSocketTcpClient client = connectedWebSockets[i];
                if (client.IsReady)
                {
                    Console.WriteLine("Writing " + data.Length + " bytes");
                    client.Write(data, asTextFrames);
                }
            }
        }
    }
}
