using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;

namespace webapi
{
    public class VideoTcpListener
    {
        public string group_id { get; set; } = "NNN";
        public TcpListener _jetsonClientListener {get; set;}
        public VideoServer? jetson_server {get; set;} = null;

        private IVideoBusService _videoBusService {get;}

        public VideoTcpListener(IVideoBusService videoBusService, int serviceport)
        {
            _jetsonClientListener = new TcpListener(IPAddress.Any, serviceport);
            _videoBusService = videoBusService;

            _jetsonClientListener.Start();
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


        public async Task TaskJetson()
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

                        EcodroneBoatMessage? message = null;
                        Debug.WriteLine(m);
                        message = JsonConvert.DeserializeObject<EcodroneBoatMessage>(m);

                        if(message != null)
                        {
                            switch (message.scope)
                            {
                                case 'U':
                                {
                                    jetson_server.SetVideoBus(_videoBusService);

                                    if (jetson_server.listener_videoBus != null)
                                    {
                                        jetson_server.listener_videoBus.Subscribe(jetson_server.ReadAndSendJetson, "jetson_id");
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

        

    	public void ListenJetson(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Accept a new client connection
                TcpClient sock_et = _jetsonClientListener.AcceptTcpClient();
                

                if (sock_et.Connected)
                {
                    Task single_client_task = Task.Factory.StartNew(async () =>
                    {
                        jetson_server = new VideoServer
                        {
                            sock_et = sock_et,
                            uuid = "jetson_id"
                        };
                        await TaskJetson();
                    });
                }

            }
        }
        

    }
}
