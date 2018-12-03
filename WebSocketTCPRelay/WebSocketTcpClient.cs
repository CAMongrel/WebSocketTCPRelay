using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace WebSocketTCPRelay
{
    internal delegate void WillClose(TcpClient client, WebSocketTcpClient webSocketTcpClient);
    internal delegate void DidClose(EndPoint closedEndPoint, WebSocketTcpClient webSocketTcpClient);
    internal delegate void DidRead(byte[] data);
    internal delegate bool ShouldAcceptOrigin(string origin);

    enum WebSocketOpCode : byte
    {
        ContinuationFrame = 0x0,

        TextFrame = 0x1,

        BinaryFrame = 0x2,

        Close = 0x8,

        Ping = 0x9,

        Pong = 0xA
    }

    /// <summary>
    /// See section 7.4. of https://tools.ietf.org/html/rfc6455
    /// </summary>
    enum WebSocketStatusCode : ushort
    {
        NormalClosure = 1000,

        GoingAway = 1001,

        ProtocolError = 1002,

        IncorrectData = 1003,

        Reserved1004 = 1004,

        ReservedNoStatus = 1005,

        ReservedAbnormal = 1006,

        InconsistentData = 1007,

        PolicyViolation = 1008,

        MessageTooBig = 1009,

        ExtensionNegotiationExpected = 1010,

        UnexpectedCondition = 1011,

        ReservedTLSHandshakeFailed = 1015,
    }

    /// <summary>
    /// Web socket tcp client.
    /// 
    /// See https://tools.ietf.org/html/rfc6455
    /// </summary>
    class WebSocketTcpClient
    {
        private const string httpEOL = "\r\n";  // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

        internal event WillClose OnWillClose;
        internal event DidClose OnDidClose;
        internal event DidRead OnDidRead;
        internal event ShouldAcceptOrigin OnShouldAcceptOrigin;

        private TcpClient client;
        private Stream stream;
        private ByteBuffer byteBuffer;
        private ByteBuffer receiveFragmentBuffer;
        private bool isSecureClient;

        private bool didSendClose;

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

        public WebSocketTcpClient(TcpClient tcpClient, bool useTLS)
        {
            isSecureClient = useTLS;
            didSendClose = false;

            IsReady = false;
            tempBuffer = new byte[32768];
            client = tcpClient;

            byteBuffer = new ByteBuffer();
            receiveFragmentBuffer = new ByteBuffer();

            var theStream = client.GetStream();
            if (isSecureClient)
            {
                stream = new SslStream(theStream, false, HandleRemoteCertificateValidationCallback);
            }
            else
            {
                stream = theStream; 
            }
        }

        public void Start()
        {
            stream.BeginRead(tempBuffer, 0, tempBuffer.Length, ReadCallback, null);
        }

        public void Stop()
        {
            Close();
        }

        private void SendCloseFrame()
        {
            if (didSendClose == true)
            {
                return; 
            }

            didSendClose = true;
        }

        private void Close()
        {
            if (IsReady)
            {
                SendCloseFrame();
            }
            IsReady = false;

            stream?.Flush();

            EndPoint endPoint = client?.Client?.RemoteEndPoint;

            OnWillClose?.Invoke(client, this);

            client?.Close();

            OnDidClose?.Invoke(endPoint, this);

            client = null;
        }

        public void Write(byte[] data, bool asTextFrames)
        {
            if (client.Connected == false)
            {
                Close();
                return;
            }

            if (IsReady && stream.CanWrite)
            {
                stream.Write(EncodeData(data, asTextFrames));
            }
        }

        private byte[] EncodeData(byte[] data, bool asTextFrames, bool isFinal = true)
        {
            using (MemoryStream memStream = new MemoryStream(data.Length + 10))
            {
                //byte firstByte = (byte)((byte)0x80 | (asTextFrames ? 0x1 : 0x2));
            }

            int reqHeaderBytes = 2;

            byte firstByte = (byte)((byte)0x80 | (asTextFrames ? 0x1 : 0x2));
            byte secondByte = 0;

            if (data.Length < 126)
            {
                secondByte = (byte)data.Length;
                reqHeaderBytes = 2;
            }
            else if (data.Length >= 126 && data.Length <= ushort.MaxValue)
            {
                secondByte = (byte)126;
                reqHeaderBytes = 4;
            }
            else
            {
                secondByte = (byte)127;
                reqHeaderBytes = 10;
            }

            byte[] res = new byte[data.Length + reqHeaderBytes];

            res[0] = firstByte;
            res[1] = secondByte;
            if (reqHeaderBytes == 4)
            {
                res[2] = (byte)((data.Length >> 8) & 0xFF);
                res[3] = (byte)(data.Length & 0xFF);
            }
            else if (reqHeaderBytes == 10)
            {
                res[2] = (byte)((data.Length >> 56) & 0xFF);
                res[3] = (byte)((data.Length >> 48) & 0xFF);
                res[4] = (byte)((data.Length >> 40) & 0xFF);
                res[5] = (byte)((data.Length >> 32) & 0xFF);
                res[6] = (byte)((data.Length >> 24) & 0xFF);
                res[7] = (byte)((data.Length >> 16) & 0xFF);
                res[8] = (byte)((data.Length >> 8) & 0xFF);
                res[9] = (byte)(data.Length & 0xFF);
            }

            Array.Copy(data, 0, res, reqHeaderBytes, data.Length);

            return res;
        }

        private byte[] DecodeData(byte[] data, out bool isFinal, out WebSocketOpCode rcvOpcode)
        {
            rcvOpcode = WebSocketOpCode.Close;
            isFinal = false;
            if (data.Length < 2)
            {
                return null;
            }

            isFinal = (data[0] >> 7) > 0;
            rcvOpcode = (WebSocketOpCode)(data[0] & 0xF);
            Console.WriteLine("isFin: " + isFinal);
            Console.WriteLine("opcode: " + rcvOpcode);

            bool isControl = rcvOpcode == WebSocketOpCode.Close ||
                             rcvOpcode == WebSocketOpCode.Ping ||
                             rcvOpcode == WebSocketOpCode.Pong;

            int maskStart = 2;
            bool isMasked = (data[1] >> 7) > 0;
            Console.WriteLine("isMasked: " + isMasked);

            if (isMasked == false)
            {
                Console.WriteLine("ERROR: Frame from client is NOT masked. Closing connection.");
                return null;
            }

            long len = (byte)(data[1] - 128);
            if (len == 126)
            {
                len = data[2] << 8 + data[3];
                maskStart = 4;
            }
            else if (len == 127)
            {
                len = data[2] << 56 + data[3] << 48 + data[4] << 40 + data[5] << 32 + data[2] << 24 + data[3] << 16 + data[4] << 8 + data[5];
                maskStart = 10;
            }
            Console.WriteLine("len: " + len);

            if (isControl && len > 125)
            {
                Console.WriteLine("ERROR: Received control opcode with payload length > 125 -> Close");
                return null;
            }

            byte[] mask = new byte[4] { data[maskStart + 0], data[maskStart + 1], data[maskStart + 2], data[maskStart + 3] };

            byte[] outData = new byte[len];
            Array.Copy(data, maskStart + 4, outData, 0, len);

            for (int i = 0; i < len; i++)
            {
                outData[i] = (byte)(outData[i] ^ mask[i % 4]);
            }

            return outData;
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            if (client.Connected == false ||
                stream == null ||
                stream.CanRead == false)
            {
                Close();
                return;
            }

            int didRead = stream.EndRead(asyncResult);

            Console.WriteLine("didRead: " + didRead);

            byteBuffer.Write(tempBuffer, 0, didRead);

            if (IsReady == false)
            {                
                if (HandleSetUpPhase() == false)
                {
                    // Send "bad request"
                    byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 400" + httpEOL + httpEOL);
                    stream.Write(response);

                    Close();
                    return;
                }
            }
            else
            {
                byte[] data = byteBuffer.GetBufferBytesCopy();

                Console.WriteLine("Received " + data.Length + " bytes");

                byte[] decodedData = DecodeData(data, out bool isFinal, out WebSocketOpCode rcvOpcode);
                if (decodedData == null)
                {
                    // Unable to decode (e.g. not masked) -> Close connection
                    Close();
                    return; 
                }

                bool isControl = rcvOpcode == WebSocketOpCode.Close ||
                                 rcvOpcode == WebSocketOpCode.Ping ||
                                 rcvOpcode == WebSocketOpCode.Pong;
                if (isControl)
                { 
                    if (isFinal == false)
                    {
                        // Control messages cannot be fragmented
                        Console.WriteLine("ERROR: Received fragmented control message -> Close");
                        Close();
                        return; 
                    }

                    Console.WriteLine($"Received control message of type '{rcvOpcode}' with length {decodedData.Length}");

                    if (rcvOpcode == WebSocketOpCode.Close)
                    {
                        if (decodedData.Length >= 2)
                        {
                            WebSocketStatusCode code = (WebSocketStatusCode)(decodedData[0] << 8 | decodedData[1]);
                            Console.WriteLine("Received close frame - code: " + code);
                        }

                        // Close() will send a close frame if necessary
                        Close();
                        return;
                    }
                }
                else
                {
                    if (isFinal == true && receiveFragmentBuffer.Length == 0)
                    {
                        // If this is a "final" block and we have no previously
                        // buffered fragments, we can send this block directly
                        // to upstream.
                        OnDidRead?.Invoke(decodedData);
                    }
                    else
                    {
                        // Cache received data until we receive the final frame
                        receiveFragmentBuffer.Write(decodedData);
                        if (isFinal == true)
                        {
                            OnDidRead?.Invoke(receiveFragmentBuffer.GetBufferBytesCopy());
                            receiveFragmentBuffer.Clear(true);
                        }
                    }
                }

                byteBuffer.Clear();
            }

            if (IsReady)
            {
                // Only try to read if we're still ready
                stream.BeginRead(tempBuffer, 0, tempBuffer.Length, ReadCallback, null);
            }
        }

        private bool HandleSetUpPhase()
        {
            if (byteBuffer.Length >= 3)
            {
                byte[] dataCopy = byteBuffer.GetBufferBytesCopy();

                string dataStr = Encoding.UTF8.GetString(dataCopy);

                if (dataStr.StartsWith("GET", StringComparison.InvariantCulture))
                {
                    try
                    {
                        string httpVer = new Regex(@"GET /([\w]*)?\s*(.*)").Match(dataStr).Groups[2].Value.Trim();
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

                        if (string.IsNullOrWhiteSpace(Origin) == false && 
                            OnShouldAcceptOrigin != null)
                        {
                            bool result = OnShouldAcceptOrigin.Invoke(Origin);
                            if (result == false)
                            {
                                Console.WriteLine($"Server rejected origin '{Origin}'");
                                return false;
                            }
                        }

                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + httpEOL
                            + "Connection: Upgrade" + httpEOL
                            + "Upgrade: websocket" + httpEOL
                            + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                                System.Security.Cryptography.SHA1.Create().ComputeHash(
                                    Encoding.UTF8.GetBytes(SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
                                )
                            ) + httpEOL
                            + httpEOL);

                        stream.Write(response);

                        byteBuffer.Clear();

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

        private bool HandleRemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // TODO: Implement!
            return true;
        }

    }
}
