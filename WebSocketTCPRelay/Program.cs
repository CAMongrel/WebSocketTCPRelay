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
        private static TcpListener tcpListener;
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

            //byte[] imageData = File.ReadAllBytes("d:\\test.png");

            webSocketServer = new WebSocketServer(webSocketListenPort, false);
            webSocketServer.Start();

            Console.WriteLine("Waiting for connection ...");

            int count = 0;

            while (running)
            {
                Thread.Sleep(10);

                count++;

                if (count >= 200)
                {
                    //webSocketServer.Write(imageData);
                    //webSocketServer.Write(Encoding.UTF8.GetBytes("file:///D:/test.png"), true);
                    webSocketServer.Write(Encoding.UTF8.GetBytes("Hallo Welt!"), true);

                    count = 0;
                }
            }

            Console.WriteLine("Shutting down");

            webSocketServer.Stop();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            running = false;
            e.Cancel = true;
        }
    }
}
