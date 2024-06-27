using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Http.Features;

namespace webapi
{
    public class JetsonVideoSocketListener
    {
        public string idjetson { get; set; } = "jetson_id";
        // public TcpListener _jetsonClientListener {get; set;}
        //public VideoServer? jetson_server {get; set;} = null;
        private IVideoBusService _videoBusService {get;}
        //public Task? main_video_task {get; set;} = null;

        public CancellationTokenSource src_cts_read = new CancellationTokenSource();
        public CancellationToken cts_read {get; private set;}


        public TcpClient? socketJetson { get; set; } = null;
        public NetworkStream? jetsonNetworkStream { get; set; } = null;
        public IVideoBusService? listener_videoBus { get; private set; }
        public Task? taskjetson {get; set;}

        public JetsonVideoSocketListener(IVideoBusService videoBusService, int serviceport, string jetson_id)
        {
            // _jetsonClientListener = new TcpListener(IPAddress.Any, serviceport);

            _videoBusService = videoBusService;
            cts_read = src_cts_read.Token;
            idjetson = jetson_id;
            // _jetsonClientListener.Start();
            Debug.WriteLine("Video process constructed");

        }


        //EVERYTHING BELONGING TO THE CLIENT THREAD AND THE HTTP LISTENER MUST BE MOVED ON THE BOAT MESSAGE LISTENER


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

        public async void ReadAndSendJetson(EcodroneBoatMessage v)
        {
            if (v.direction == idjetson)
            {
                if (jetsonNetworkStream != null)
                {
                    try
                    {
                        string message_out = JsonConvert.SerializeObject(v);
                        byte[] message = Encoding.UTF8.GetBytes(message_out);
                        await jetsonNetworkStream.WriteAsync(message);

                    }catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
        }
    


        public async Task TaskJetson()
        {
            if (socketJetson != null)
            {
                if(socketJetson.Connected)
                {
                    jetsonNetworkStream = socketJetson.GetStream();
                    
                    while(socketJetson.Connected)
                    { 
                        int bytesRead = -1;
                        byte[] buffer = new byte[16384];

                       
                        bytesRead = await jetsonNetworkStream.ReadAsync(buffer, cts_read);

                        if(!cts_read.IsCancellationRequested)
                        {
                            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            List<string> messages = ExtractJsonObjects(receivedMessage);

                            foreach (var m in messages)
                            {

                                EcodroneBoatMessage? message = null;
                                Debug.WriteLine(m);
                                message = JsonConvert.DeserializeObject<EcodroneBoatMessage>(m);

                                if(message != null)
                                {
                                    switch (message.scope)
                                    {
                                        case 'U':
                                        {
                                            _videoBusService.Subscribe(ReadAndSendJetson, "jetson_id");

                                            EcodroneBoatMessage startMessage = new EcodroneBoatMessage(){
                                                scope = 'X',
                                                type = "1",
                                                uuid = "main",
                                                direction = idjetson,
                                                identity = "NNN",
                                                data = null
                                            };

                                            await Task.Delay(100);
                                            _videoBusService.Publish(startMessage);
                                        }
                                        break;
                                        case 'X':
                                            if(message.type == "0")
                                            {
                                                //src_cts_jetson.Cancel();

                                                
                                            }
                                        break;
                                        case 'T':
                                            
                                            string msg_data = message.type.ToString();
                                            long dim =  long.Parse(msg_data);
                                           
                                            long indext = 0;
                                            byte[] arrdata = new byte[dim];

                                            while(indext != dim)
                                            {
                                                byte[] buffert = new byte[16000];
                                                int bread = await jetsonNetworkStream.ReadAsync(buffert, cts_read);
                                                
                                                Array.Copy(buffert, 0, arrdata, indext, bread );
                                                if(indext > 1030000)
                                                {
                                                    Debug.WriteLine("lots reached");
                                                }
                                                indext += bread;
                                            }

                                            //Debug.WriteLine(arrdata);
                                            string filePath = "outputVideoFile.mp4"; // Replace with your desired file name and extension

                                            try
                                            {
                                                File.WriteAllBytes(filePath, arrdata);
                                                Console.WriteLine("File written successfully to " + filePath);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine("An error occurred while writing the file: " + ex.Message);
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
            }
        }


        public void ConnectToJetsonVideoSocket()
        {
            //manage here if no video connected
            //IPEndPoint iPEndPoint = new IPEndPoint(, 5057);
            socketJetson = new TcpClient("2.194.19.139", 5057);
            
            if (socketJetson.Connected)
            {
                // NetworkStream? _networkStream = newclient.GetStream();
                taskjetson = Task.Run(TaskJetson);
            }

            

        }
        // public void OnClientConnect(IAsyncResult ar)
        // {
        //     //manage here if no video connected
        //     TcpClient newclient = _jetsonClientListener.EndAcceptTcpClient(ar);
        
        //     if (newclient.Connected)
        //     {
        //         Task.Run(async () =>
        //         {
        //             jetson_server = new VideoServer
        //             {
        //                 sock_et = newclient,
        //                 uuid = "jetson_id"
        //             };
                    
        //             await TaskJetson();
        //         }, cts_jetson);

        //     }

        // }
        

    }
}
