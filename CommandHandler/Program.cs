using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CommandHandler
{
    class Program
    {
        private static object lockObj;

        private static TcpClient client;
        private static NetworkStream stream;
        private static bool running = true;

        private static byte[] buffer;

        private static Queue<string> receivedStrings;

        static void Main(string[] args)
        {
            lockObj = new object();

            receivedStrings = new Queue<string>();
            buffer = new byte[2048];

            client = new TcpClient("localhost", 9001);
            stream = client.GetStream();
            stream.BeginRead(buffer, 0, buffer.Length, HandleAsyncCallback, stream);

            while (running)
            {
                Thread.Sleep(10);

                bool res = false;
                string data = null;

                lock (lockObj)
                {
                    res = receivedStrings.TryDequeue(out data);
                }

                if (res)
                {
                    HandleString(data);
                }
            }

            Console.WriteLine("Shutting down");
        }

        private static void HandleString(string data)
        {
            Console.WriteLine("Handle: " + data);

            try
            {
                Command cmd = Newtonsoft.Json.JsonConvert.DeserializeObject<Command>(data);
                if (cmd == null)
                {
                    Console.WriteLine("Cannot handle null Command");
                    return; 
                }
                HandleCommand(cmd);
            }
            catch (Newtonsoft.Json.JsonException ex)
            {
                SendCommand(new Command() { Cmd = "error", Parameters = { { "error", "Failed to parse JSON." } } });

                Console.WriteLine("Failed to parse json: " + ex);
            }
        }

        private static void HandleCommand(Command cmd)
        {
            switch (cmd.Cmd)
            {
                case "error":
                    Console.WriteLine("Received error message: " + cmd["error"]);
                    break;

                case "message":
                    Console.WriteLine("Received message: " + cmd["message"]);
                    break;

                case "list":
                    string folder = cmd["folder"];
                    if (folder == null ||
                        Directory.Exists(folder) == false)
                    {
                        folder = "~";
                    }

                    string[] files = Directory.GetFiles(folder);

                    Command command = new Command();
                    command.Cmd = "listresult";
                    for (int i = 0; i < files.Length; i++)
                    {
                        command.Parameters.Add("file" + i, files[i]);
                    }
                    SendCommand(command);
                    break;

                default:
                    SendCommand(new Command() { Cmd = "error", Parameters = { { "error", "Unknown command: " + cmd.Cmd } } });
                    break;
            }
        }

        private static void SendCommand(Command cmd)
        {
            string data = Newtonsoft.Json.JsonConvert.SerializeObject(cmd);
            stream.Write(Encoding.UTF8.GetBytes(data));
        }

        static void HandleAsyncCallback(IAsyncResult ar)
        {
            NetworkStream theStream = (NetworkStream)ar.AsyncState;

            int didRead = theStream.EndRead(ar);
            string data = Encoding.UTF8.GetString(buffer, 0, didRead);

            lock (lockObj)
            {
                receivedStrings.Enqueue(data);
            }

            stream.BeginRead(buffer, 0, buffer.Length, HandleAsyncCallback, theStream);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            running = false;
            e.Cancel = true;
        }
    }
}
