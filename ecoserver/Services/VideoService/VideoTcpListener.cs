using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.DataProtection;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.VisualBasic;
using System.Net.WebSockets;

namespace webapi
{
    public class VideoTcpListener
    {
        public string group_id { get; set; } = "NNN";
        //public List<VideoListener> videoListeners { get; set; } = new List<VideoListener>();
        public IVideoBusService _videoBusService { get; set; } = new VideoBusService();
        //private IVideoSocketSingleton _videoService { get; set; }
        
        private TcpListener _tcpListener {get; set;}
        // private Socket? _jetsonSocket {get; set;} = null;
        // private NetworkStream? _jetsonStream {get; set;} = null;
        private VideoServer? jetson_server {get; set;} = null;
        private HttpListener _httpListener { get; set; }
        private Thread clientThread { get; set; }
        private Thread jetsonThread { get; set; }
      
        public VideoTcpListener(int port = 5055)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://*:5055/"); 
            _tcpListener = new TcpListener(IPAddress.Any, 5057);

            Debug.WriteLine("process initiated or at least called");
            jetsonThread = new Thread(new ThreadStart(ListenJetson));
            clientThread = new Thread(new ThreadStart(ListenForClients));
            
            _tcpListener.Start();
            _httpListener.Start();
            jetsonThread.Start();
            clientThread.Start();
            
        }

        private List<string> ExtractJsonObjects(string input)
        {
            List<string> objects = new List<string>();
            int startIndex = 0;
            Stack<char> stack = new Stack<char>();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    if (stack.Count == 0)
                    {
                        startIndex = i;
                    }
                    stack.Push(input[i]);
                }
                else if (input[i] == '}')
                {
                    if (stack.Count == 1)
                    {
                        string obj = input.Substring(startIndex, i - startIndex + 1);
                        objects.Add(obj);
                    }
                    stack.Pop();
                }
            }

            return objects;
        }

        private string ProcessSocketMessage(byte[] data_buff_array)
        {

            byte opcode = (byte)(data_buff_array[0] & 0x0F);
            /* 
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
            */
            //
            byte[] maskingKey = new byte[4];

            /*        byte[] data_buff_array = new byte[bytesRead];
                    Array.Copy(buffer, 0, data_buff_array, 0, bytesRead);*/
            string stringmessage = string.Empty;
            switch (opcode)
            {
                case 0x0:
                    Debug.WriteLine("Continuation frame");
                    break;
                case 0x1:
                    Debug.WriteLine("Text Frame");
                    {

                        int payloadLength = data_buff_array[1] & 0x7F;
                        int index_buffer = 2;


                        // If the payload length is 126, the next 2 bytes represent the actual payload length
                        if (payloadLength == 126)
                        {
                            payloadLength = BitConverter.ToUInt16([data_buff_array[2], data_buff_array[3]], 0);
                            index_buffer = 4;
                        }
                        // If the payload length is 127, the next 8 bytes represent the actual payload length
                        else if (payloadLength == 127)
                        {
                            payloadLength = (int)BitConverter.ToUInt64([data_buff_array[2], data_buff_array[3], data_buff_array[4], data_buff_array[5], data_buff_array[6], data_buff_array[7], data_buff_array[8], data_buff_array[9]], 0);
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
                    break;
                    case 0x2:
                        Debug.WriteLine("Binary Message");
                        break;
                    case 0x8:
                        Debug.WriteLine("This is a connection");
                        break;
                    case 0x9:
                        Debug.WriteLine("This is a ping or a pong");
                        break;
                    case 0x3:
                    case 0xB:
                    case 0xF:
                        Debug.WriteLine("Reserved control frame");
                        break;
            }
            
            return stringmessage;
        }

        public async Task TaskJetson(VideoServer jetson_server)
        {
            if (jetson_server != null && jetson_server.sock_et != null)
            {
                jetson_server.networkStream = jetson_server.sock_et.GetStream();
                int bytesRead = -1;
                byte[] buffer = new byte[16384];
            
                while ((bytesRead = await jetson_server.networkStream.ReadAsync(buffer)) > 0)
                {

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    List<string> messages = ExtractJsonObjects(receivedMessage);

                    foreach (var m in messages)
                    {

                        VideoMessage? message = null;
                        Debug.WriteLine(m);
                        message = JsonSerializer.Deserialize<VideoMessage>(m);

                        if(message != null && message.direction != "server")
                        {
                            switch (message.scope)
                            {
                                case 'U':
                                    {
                                        jetson_server.SetVideoBus(_videoBusService);

                                        if (jetson_server.listener_videoBus != null)
                                        {
                                            jetson_server.listener_videoBus.Subscribe(jetson_server.ReadAndSendJetson);
                                        }
                                    }
                                    break;
                                default:
                                    {
                                        await Task.Delay(100);
                                        _videoBusService.Publish(message);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public async Task TaskSocket(VideoClientListener video_listener)
        {
            if (video_listener._websocket != null && video_listener._websocket.State == WebSocketState.Open)
            {
                Debug.WriteLine("called");
                byte[] buffer = new byte[16384];
                int isHandshake = -1;
                string receivedMessage = string.Empty;

                while(true)
                {
                    WebSocketReceiveResult receiveResult = await video_listener._websocket.ReceiveAsync(buffer, CancellationToken.None);

                    switch (receiveResult.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            {
                                receivedMessage = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                                Array.Clear(buffer);
                                Debug.WriteLine("message is : ", receivedMessage);
                                List<string> messages = ExtractJsonObjects(receivedMessage);

                                foreach (var m in messages)
                                {
                                    Debug.WriteLine("message is: ", m);
                                    VideoMessage? videoMessage = null;

                                    try
                                    {
                                        videoMessage = JsonSerializer.Deserialize<VideoMessage>(m);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"{ex.Message}");  
                                    }
                                    

                                    if (videoMessage != null)
                                    {
                                        switch (videoMessage.scope)
                                        {
                                            case 'U':
                                                {
                                                    video_listener.uuid = Guid.NewGuid().ToString();
                                                    video_listener.SetVideoBus(_videoBusService);


                                                    videoMessage.scope = 'U';
                                                    videoMessage.type = "1";
                                                    videoMessage.uuid = video_listener.uuid;
                                                    videoMessage.identity = video_listener.uuid;
                                                    videoMessage.data = "data";
                                                    videoMessage.direction = video_listener.uuid;

                                                    if (video_listener.listener_videoBus != null)
                                                    {
                                                        video_listener.listener_videoBus.Subscribe(video_listener.ReadAndSendClient);
                                                        byte[] data_message = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(videoMessage));
                                                    
                                                        await video_listener._websocket.SendAsync(data_message, WebSocketMessageType .Text, true, CancellationToken.None);
                                                        videoMessage.direction = "server";
                                                        video_listener.listener_videoBus.Publish(videoMessage);
                                                        videoMessage = null;
                                                    }
                                                }
                                                break;
                                            default:
                                                {
                                                    await Task.Delay(100);
                                                    _videoBusService.Publish(videoMessage);
                                                }
                                                break;
                                        }
                                    }
                                }
                            }

                            break;
                        case WebSocketMessageType.Binary:
                            byte[] exact_message_bytes = new byte[receiveResult.Count];
                            Array.Copy(buffer, exact_message_bytes, exact_message_bytes.Length);
                            break;
                        case WebSocketMessageType.Close:
                            Debug.WriteLine("closing message", video_listener._websocket.CloseStatus);
                            video_listener._websocket.Dispose();
                            break;
                        
                    }
                }
                
                

            }

        }

    	public void ListenJetson()
        {
            while (true)
            {
                // Accept a new client connection
                TcpClient sock_et = _tcpListener.AcceptTcpClient();
                

                if (sock_et.Connected)
                {
                    Task single_client_task = Task.Factory.StartNew(async () =>
                    {
                        VideoServer video_listener = new VideoServer
                        {
                            sock_et = sock_et,
                            uuid = "server"
                        };
                        await TaskJetson(video_listener);
                    });
                }

            }
        }
        public async void ListenForClients()
        {
            while (true)
            {
                // Accept a new client connection
                //Socket sock_et = tcpListener.AcceptSocket();
                Debug.WriteLine("accept ready here");
                HttpListenerContext _client_context = await _httpListener.GetContextAsync();
                Debug.WriteLine("accepted");
                Task single_client_task = Task.Factory.StartNew(async () =>
                    {
                        HttpListenerWebSocketContext websocket_context = await _client_context.AcceptWebSocketAsync(null);
                        Debug.WriteLine("it is client here");

                        if (websocket_context.IsSecureConnection)
                        {
                            Debug.WriteLine("is secure connection");
                        }
                        if(websocket_context.IsAuthenticated)
                        {
                            Debug.WriteLine("connection is authenticated");
                        }

                        Debug.WriteLine(websocket_context.SecWebSocketKey);
                        Debug.WriteLine(websocket_context.SecWebSocketVersion);
                        Debug.WriteLine(websocket_context.User);

                        
                        Task single_client_task = Task.Factory.StartNew(async () =>
                        {
                            VideoClientListener video_listener = new VideoClientListener
                            {
                                _websocket = websocket_context.WebSocket
                                
                            };
                            await TaskSocket(video_listener);
                        });
                    });
                


                // if (sock_et.Connected)
                // {
                //     
                // }

            }
        }

    }
}
