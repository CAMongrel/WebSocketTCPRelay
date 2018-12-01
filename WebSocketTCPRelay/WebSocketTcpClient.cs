using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSocketTCPRelay
{
    internal delegate void DidClose(TcpClient client, WebSocketTcpClient webSocketTcpClient);
    internal delegate void DidRead(byte[] data);

    class WebSocketTcpClient
    {
        internal event DidClose OnDidClose;
        internal event DidRead OnDidRead;

        private TcpClient client;
        private NetworkStream stream;
        private ByteBuffer byteBuffer;

        private byte[] tempBuffer;

        public bool IsReady { get; private set; }

        public string Host { get; private set; }
        public string Accept { get; private set; }
        public string UserAgent { get; private set; }
        public string AcceptLanguage { get; private set; }
        public string AcceptEncoding { get; private set; }
        public string Origin { get; private set; }
        public string SecWebSocketVersion { get; private set; }
        public string SecWebSocketExtensions { get; private set; }
        public string SecWebSocketKey { get; private set; }
        public string Pragma { get; private set; }
        public string Connection { get; private set; }
        public string CacheControl { get; private set; }

        public WebSocketTcpClient(TcpClient tcpClient)
        {
            IsReady = false;
            tempBuffer = new byte[32768];
            client = tcpClient;

            byteBuffer = new ByteBuffer();

            stream = client.GetStream();
        }

        public void Start()
        {
            stream.BeginRead(tempBuffer, 0, tempBuffer.Length, ReadCallback, null);
        }

        public void Write(byte[] data, bool asTextFrames)
        {
            if (client.Connected == false)
            {
                OnDidClose?.Invoke(client, this);
                return;
            }

            if (stream.CanWrite)
            {
                stream.Write(EncodeData(data, asTextFrames));
            }
        }

        private byte[] EncodeData(byte[] data, bool asTextFrames)
        {
            byte[] res = new byte[data.Length + 2];

            res[0] = (byte)((1 << 7) + (asTextFrames ? 0x2 : 0x1));

            // TODO: Check if length > 125 etc.
            res[1] = (byte)data.Length;

            Array.Copy(data, 0, res, 2, data.Length);

            return res;
        }

        private byte[] DecodeData(byte[] data)
        { 
            if (data.Length < 2)
            {
                return data;
            }

            bool isFin = (data[0] >> 7) > 0;
            byte opcode = (byte)(data[0] & 0xF);
            Console.WriteLine("isFin: " + isFin);
            Console.WriteLine("opcode: " + opcode);

            int maskStart = 2;
            bool isMasked = (data[1] >> 7) > 0;
            Console.WriteLine("isMasked: " + isMasked);
            int len = (byte)(isMasked ? data[1] - 128 : data[1]);
            if (len == 126)
            {
                len = data[2] << 8 + data[3];
                maskStart = 4;
            }
            else if (len == 127)
            {
                len = data[2] << 24 + data[3] << 16 + data[4] << 8 + data[5];
                maskStart = 6;
            }
            Console.WriteLine("len: " + len);
            if (isMasked == false)
            {

            }
            else
            {

            }
            byte[] mask = 

            return data;
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            int didRead = stream.EndRead(asyncResult);

            byteBuffer.Write(tempBuffer, 0, didRead);

            if (IsReady == false)
            {
                if (HandleSetUpPhase() == false)
                {
                    client.Close();

                    OnDidClose?.Invoke(client, this);

                    client = null;
                    return;
                }
            }
            else
            {
                byte[] data = byteBuffer.GetBufferBytesCopy();

                Console.WriteLine("Received " + data.Length + " bytes");

                OnDidRead?.Invoke(DecodeData(data));

                byteBuffer.Clear();
            }

            stream.BeginRead(tempBuffer, 0, tempBuffer.Length, ReadCallback, null);
        }

        private bool HandleSetUpPhase()
        {
            if (byteBuffer.Length >= 3)
            {
                byte[] dataCopy = byteBuffer.GetBufferBytesCopy();

                string dataStr = Encoding.UTF8.GetString(dataCopy);

                if (dataStr.StartsWith("GET"))
                {
                    try
                    {
                        string httpVer = new Regex(@"GET / (.*)").Match(dataStr).Groups[1].Value.Trim();
                        if (httpVer != "HTTP/1.1")
                        {
                            Console.WriteLine("ERROR: Can only handle HTTP/1.1 request");
                            return false;
                        }

                        Host = GetOptionalValue("Host", dataStr);
                        UserAgent = GetOptionalValue("User-Agent", dataStr);
                        Accept = GetOptionalValue("Accept", dataStr);
                        AcceptLanguage = GetOptionalValue("Accept-Language", dataStr);
                        AcceptEncoding = GetOptionalValue("Accept-Encoding", dataStr);
                        Origin = GetOptionalValue("Origin", dataStr);
                        SecWebSocketVersion = GetOptionalValue("Sec-WebSocket-Version", dataStr);
                        SecWebSocketExtensions = GetOptionalValue("Sec-WebSocket-Extensions", dataStr);
                        SecWebSocketKey = GetOptionalValue("Sec-WebSocket-Key", dataStr);
                        Connection = GetOptionalValue("Connection", dataStr);
                        Pragma = GetOptionalValue("Pragma", dataStr);
                        CacheControl = GetOptionalValue("Cache-Control", dataStr);
                        string upgrade = GetOptionalValue("Upgrade", dataStr);

                        if (upgrade != "websocket")
                        {
                            Console.WriteLine("ERROR: Upgrade != 'websocket'");
                            return false;
                        }

                        const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                            + "Connection: Upgrade" + eol
                            + "Upgrade: websocket" + eol
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
                                )
                            ) + eol
                            + eol);

                        stream.Write(response);

                        IsReady = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: Exception: " + ex);
                        return false;
                    }
                }
            }

            return true;
        }

        private string GetOptionalValue(string prefix, string content)
        {
            try
            {
                Match match = new Regex(prefix + @":\s*(.*)").Match(content);
                if (match.Groups.Count < 2)
                {
                    return null;
                }
                string res = match.Groups[1].Value.Trim();
                //Console.WriteLine(prefix + " == " + res);
                return res;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
