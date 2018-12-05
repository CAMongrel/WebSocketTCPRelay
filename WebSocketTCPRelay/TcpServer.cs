using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace WebSocketTCPRelay
{
    /// <summary>
    /// "data" is invalid after the call and cannot be used 
    /// asynchronously afterwards.
    /// </summary>
    internal delegate void DidReadBytes(WrappedTcpClient client, ReadOnlySpan<byte> data);

    internal class WrappedTcpClient
    {
        private TcpClient client;
        private NetworkStream stream;
        private byte[] buffer;

        internal event DidReadBytes OnDidReadBytes;

        public bool IsConnected
        {
            get 
            {
                return stream != null &&
                    client != null &&
                    client.Connected &&
                    stream.CanWrite;
            }
        }

        internal WrappedTcpClient(TcpClient setClient)
        {
            buffer = new byte[2048];
            client = setClient;
            stream = client.GetStream();
        }

        internal void Start()
        {
            stream.BeginRead(buffer, 0, buffer.Length, HandleAsyncCallback, null);
        }

        private void HandleAsyncCallback(IAsyncResult ar)
        {
            int didRead = stream.EndRead(ar);
            if (didRead == 0)
            {
                Console.WriteLine("Did read 0 bytes");

                stream = null;

                client?.Close();
                client = null;

                return; 
            }

            Span<byte> span = new Span<byte>(buffer, 0, didRead);
            OnDidReadBytes?.Invoke(this, span);
            span = null;

            stream.BeginRead(buffer, 0, buffer.Length, HandleAsyncCallback, null);
        }

        internal void Write(byte[] data)
        {
            if (IsConnected)
            {
                stream.Write(data);
            }
        }
    }

    public class TcpServer
    {
        private object lockObj;

        private TcpListener tcpListener;

        private List<WrappedTcpClient> clients;

        internal event DidReadBytes OnDidReadBytes;

        public TcpServer(IPAddress listenAddress, ushort listenPort)
        {
            lockObj = new object();

            clients = new List<WrappedTcpClient>();
            tcpListener = new TcpListener(listenAddress, listenPort);
        }

        public void Start()
        {
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(HandleAsyncCallback, tcpListener);
        }

        public void Stop()
        {
            tcpListener.Stop();
        }

        private void HandleAsyncCallback(IAsyncResult ar)
        {
            if (ar.IsCompleted == false)
            {
                return;
            }

            TcpListener listener = (TcpListener)ar.AsyncState;

            var client = listener.EndAcceptTcpClient(ar);

            Console.WriteLine("TcpClient connected from " + client.Client.RemoteEndPoint);

            var wr_client = new WrappedTcpClient(client);
            wr_client.OnDidReadBytes += (theClient, data) =>
            {
                OnDidReadBytes?.Invoke(theClient, data); 
            };
            lock (lockObj)
            {
                clients.Add(wr_client);
            }
            wr_client.Start();

            tcpListener.BeginAcceptTcpClient(HandleAsyncCallback, tcpListener);
        }

        public void Write(byte[] data, bool asTextFrames = false)
        {
            lock (lockObj)
            {
                List<WrappedTcpClient> clientsToRemove = new List<WrappedTcpClient>();
                for (int i = 0; i < clients.Count; i++)
                {
                    WrappedTcpClient client = clients[i];
                    if (client.IsConnected == false)
                    {
                        clientsToRemove.Add(client);
                        continue; 
                    }
                    Console.WriteLine("Writing " + data.Length + " bytes");
                    client.Write(data);
                }

                foreach (var cli in clientsToRemove)
                {
                    Console.WriteLine("Removing client ... no longer connected");
                    clients.Remove(cli);
                }
            }
        }
    }
}
