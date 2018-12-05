using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WebSocketTCPRelay
{
    internal delegate void ReceiveBytes(ReadOnlySpan<byte> data);

    /// <summary>
    /// Web socket server.
    /// 
    /// See https://tools.ietf.org/html/rfc6455
    /// </summary>
    class WebSocketServer
    {
        internal event ReceiveBytes OnReceiveBytes;

        private TcpListener listener;
        private bool isSecureListener;

        private List<WebSocketTcpClient> connectedWebSockets;

        public WebSocketServer(ushort listenPort, bool useTLS)
        {
            isSecureListener = useTLS;
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
            TcpListener listenerInstance = (TcpListener)ar.AsyncState;

            TcpClient client = listenerInstance.EndAcceptTcpClient(ar);

            Console.WriteLine("WebSocketClient connected from " + client.Client.RemoteEndPoint);

            WebSocketTcpClient webSocketTcpClient = new WebSocketTcpClient(client, isSecureListener);
            webSocketTcpClient.OnDidClose += (endpoint, wsClient) =>
            {
                Console.WriteLine("Removing WebSocketClient ... " + endpoint);
                if (connectedWebSockets.Contains(webSocketTcpClient))
                {
                    connectedWebSockets.Remove(webSocketTcpClient);
                }
            };
            webSocketTcpClient.OnDidRead += (data) =>
            {
                OnReceiveBytes?.Invoke(data);
            };
            // TODO: Implement "CONNECTING" and "CONNECTED" states to limit
            // the number of "CONNECTING" requests from a specific client to "one"
            connectedWebSockets.Add(webSocketTcpClient);
            webSocketTcpClient.Start();

            // Wait for more clients
            listenerInstance.BeginAcceptTcpClient(AcceptTcpClientCallback, listenerInstance);
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
