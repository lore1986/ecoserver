using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace webapi.Services.SocketService
{
    /*public class VideoSocketManager
    {*/
        
        /*private byte[] PrepareMessageToBeSend(byte[] message)
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

        }*/

        /*private string ProcessSocketMessage(byte[] buffer, int bytesRead)
        {

            byte opcode = (byte)(buffer[0] & 0x0F);
            *//* 
             * Opcode: 4 bits
             * Defines the interpretation of the "Payload data".If an unknown
             * opcode is received, the receiving endpoint MUST _Fail the WebSocket Connection_.  
             * The following values are defined.
             * % x0 denotes a continuation 
             * % x1 denotes a text 
             * % x2 denotes a binary 
             * % x3 - 7 are reserved for further non-control frames
             * % x8 denotes a connection 
             * % x9 denotes a 
             * % xA denotes a 
             * % xB - F are reserved for further control frames
            *//*
            //
            byte[] maskingKey = new byte[4];

            byte[] data_buff_array = new byte[bytesRead];
            Array.Copy(buffer, 0, data_buff_array, 0, bytesRead);
            string stringmessage = string.Empty;

            if (opcode == 0x1) // Text frame
            {

                int payloadLength = buffer[1] & 0x7F;
                int index_buffer = 2;


                // If the payload length is 126, the next 2 bytes represent the actual payload length
                if (payloadLength == 126)
                {
                    payloadLength = BitConverter.ToUInt16(new byte[] { data_buff_array[2], data_buff_array[3] }, 0);
                    index_buffer = 4;
                }
                // If the payload length is 127, the next 8 bytes represent the actual payload length
                else if (payloadLength == 127)
                {
                    payloadLength = (int)BitConverter.ToUInt64(new byte[] { data_buff_array[2], data_buff_array[3], data_buff_array[4], data_buff_array[5], data_buff_array[6], data_buff_array[7], data_buff_array[8], data_buff_array[9] }, 0);
                    index_buffer = 10;
                }

                // Extract the mask (if masking is applied)
                bool isMasked = false;
                isMasked = (data_buff_array[1] & 0x80) == 0x80;

                if (isMasked)
                {
                    Array.Copy(data_buff_array, index_buffer, maskingKey, 0, 4);
                    index_buffer += 4;
                }

                // Extract the payload data
                byte[] payload = new byte[data_buff_array.Length - index_buffer];
                //Array.Copy(buffer, index_buffer + 1, payload, 0, payloadLength);
                int index_payload = 0;
                for (int i = index_buffer; i < data_buff_array.Length; i++)
                {
                    payload[index_payload] = (byte)(data_buff_array[i] ^ maskingKey[index_payload % 4]);
                    index_payload++;
                }

                // Convert the payload to a string
                stringmessage = Encoding.UTF8.GetString(payload);
                Debug.WriteLine(stringmessage);

            }

            return stringmessage;
        }*/

        /*static byte[] ProcessHandshake(string receivedMessage)
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
        }*/

        /*public async void HandleClient(Socket clientSocket)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                string message_string = string.Empty;

                while ((bytesRead = await clientSocket.ReceiveAsync(buffer)) > 0)
                {

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received message from {clientSocket.RemoteEndPoint}: {receivedMessage}");


                    if (receivedMessage.Contains("Upgrade: websocket"))
                    {

                        byte[] handshakeResponse = ProcessHandshake(receivedMessage);
                        clientSocket.Send(handshakeResponse);

                    }
                    else if (receivedMessage.Contains("scope"))
                    {
                        Debug.WriteLine(receivedMessage);
                        message_string = receivedMessage;
                    }
                    else
                    {
                        message_string = ProcessSocketMessage(buffer, bytesRead);

                    }


                    byte[] res = Encoding.UTF8.GetBytes("welcome to the loop");

                    byte[] message_to_send = PrepareMessageToBeSend(res);
                    clientSocket.Send(message_to_send);

                    Array.Clear(buffer, 0, buffer.Length);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                // Close the client socket
                //clientSocket.Close();
            }
        }*/
/*
    }*/
}
