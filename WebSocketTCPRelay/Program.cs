using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebSocketTCPRelay
{
    class Program
    {
        private static TcpServer tcpServer;
        private static WebSocketServer webSocketServer;
        private static bool running = true;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            ushort tcpListenPort = 9001;
            ushort webSocketListenPort = 54018;

            Console.WriteLine("WebSocketTCPRelay");
            Console.WriteLine("TCP listen port: " + tcpListenPort);
            Console.WriteLine("WebSocket listen port: " + webSocketListenPort);

            tcpServer = new TcpServer(IPAddress.Any, tcpListenPort);
            tcpServer.Start();
            tcpServer.OnDidReadBytes += TcpServer_OnDidReadBytes;

            webSocketServer = new WebSocketServer(webSocketListenPort, true);
            webSocketServer.OnReceiveBytes += WebSocketServer_OnReceiveBytes;
            webSocketServer.Start();

            Console.WriteLine("Waiting for connections ...");

            while (running)
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Shutting down");

            tcpServer.Stop();
            webSocketServer.Stop();
        }

        static void WebSocketServer_OnReceiveBytes(byte[] data)
        {
            tcpServer?.Write(data);
        }

        static void TcpServer_OnDidReadBytes(WrappedTcpClient client, Span<byte> data)
        {
            webSocketServer?.Write(data.ToArray());
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            running = false;
            e.Cancel = true;
        }
    }
}
