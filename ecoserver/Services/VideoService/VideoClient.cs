using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace webapi
{
    public class VideoClient : VideoListener
    {

        public async void ReadAndSendClient(VideoMessage v)
        {
            if (v.direction == uuid)
            {
                try 
                {
                    if (networkStream != null)
                    {
                        string message_out = JsonSerializer.Serialize(v);
                        byte[] message = Encoding.UTF8.GetBytes(message_out);
                        byte[] ready_message = PrepareMessageToBeSend(message);
                        await networkStream.WriteAsync(ready_message);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

     
        public byte[] ProcessHandshake(string receivedMessage)
        {
            byte[] key = new byte[4];

            string? secWebSocketKey = receivedMessage.Split('\n')
            .FirstOrDefault(line => line.StartsWith("Sec-WebSocket-Key:"))
            ?.Substring("Sec-WebSocket-Key:".Length).Trim();

            key = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            string responseKey = Convert.ToBase64String(key);


            // Build the WebSocket handshake response as bytes
            string handshakeResponse = $"HTTP/1.1 101 Switching Protocols\r\n" +
                                        $"Upgrade: websocket\r\n" +
                                        $"Connection: Upgrade\r\n" +
                                        $"Sec-WebSocket-Accept: {responseKey}\r\n\r\n";

            return Encoding.UTF8.GetBytes(handshakeResponse);
        }

        


        public byte[] PrepareMessageToBeSend(byte[] message)
        {
            int message_length = message.Length;
            int index_data = -1;
            List<byte> message_list = new List<byte>();

            message_list.Insert(0, 129);

            if (message_length <= 125)
            {
                message_list.Insert(1, (byte)message_length);
                index_data = 2;
            }
            else if (message_length >= 126 && message_length <= UInt16.MaxValue)
            {
                message_list.Insert(1, 126);
                message_list.Insert(2, (byte)((message_length >> 8) & 255));
                message_list.Insert(3, (byte)((message_length) & 255));
                index_data = 4;
            }
            else
            {
                message_list.Insert(1, 127);
                message_list.Insert(2, (byte)((message_length >> 56) & 255));
                message_list.Insert(3, (byte)((message_length >> 48) & 255));
                message_list.Insert(4, (byte)((message_length >> 40) & 255));
                message_list.Insert(5, (byte)((message_length >> 32) & 255));
                message_list.Insert(6, (byte)((message_length >> 24) & 255));
                message_list.Insert(7, (byte)((message_length >> 16) & 255));
                message_list.Insert(8, (byte)((message_length >> 8) & 255));
                message_list.Insert(9, (byte)((message_length) & 255));

                index_data = 10;
            }

            byte[] response_data = new byte[message_list.Count() + message_length];

            Array.Copy(message_list.ToArray(), 0, response_data, 0, message_list.Count());
            Array.Copy(message, 0, response_data, index_data, message_length);


            return response_data;

        }
    }
}
