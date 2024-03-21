using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using webapi;
using webapi.Utilities;

public class EcodroneBoat
{
    public string maskedId { get; set; } = "NNN";
    public byte[] _Sync { get; private set; }
    public string _IPTeensy { get; private set; }
    public int _PortTeensy { get; private set; }
    public bool isActive { get; protected set; } = false;
    private int portvideo {get; set;} = 5057;
    private int port = 5058;

    public Task httpListener_task;
    public Task jetsonListener_task;
    public HttpListener _ecodroneBoatClienSocketListener;
    public EcodroneTeensyInstance teensySocketInstance {get; protected set;}
    public List<EcoClient> _boatclients = new List<EcoClient>();
    public VideoTcpListener ecodroneVideo {get; protected set;}
    public IVideoBusService _videoBusService { get; set; } = new VideoBusService();

    public CancellationTokenSource src_cts_boat = new CancellationTokenSource();
    public CancellationToken cts_boat {get; private set;}


    public EcodroneBoat(string _maskedid, byte[] sync, string IPT, int PT, bool setActive = false)
    {
        if(sync.Length != 3) { throw new ArgumentException("teensy sync not valid");}
        _Sync = sync;
        _IPTeensy = IPT;
        _PortTeensy = PT;
        isActive = setActive;
        maskedId = _maskedid;

        _ecodroneBoatClienSocketListener = new HttpListener();
        _ecodroneBoatClienSocketListener.Prefixes.Add($"http://localhost:{port}/");

        teensySocketInstance = new EcodroneTeensyInstance(this, _maskedid);
        ecodroneVideo = new VideoTcpListener(_videoBusService, portvideo);
        
        cts_boat = src_cts_boat.Token;
    }

    public EcodroneBoat ChangeState(bool state)
    {
        isActive = state;
        return this;
    }

    public bool IsCorrectByte(byte byte_zero)
    {
        List<byte> bytes_admitted = new List<byte>
        {
            83, //STATE MANAGER
            77, //MISSION
            86, //VIDEO
            72 //WAYPOINT
        };
        
        return bytes_admitted.Contains(byte_zero);
    }
    

    public void StartEcodroneBoatTasks()
    {
       _ecodroneBoatClienSocketListener.Start();
        teensySocketInstance.TaskReading = Task.Run(teensySocketInstance.StartTeensyTalk, teensySocketInstance.cts_teensy);

        _ecodroneBoatClienSocketListener.BeginGetContext(callBack_BoatListener, _ecodroneBoatClienSocketListener);
        ecodroneVideo._jetsonClientListener.BeginAcceptTcpClient(new AsyncCallback(ecodroneVideo.OnClientConnect), ecodroneVideo._jetsonClientListener);
        //jetsonListener_task = Task.Run(() => ecodroneVideo.ListenJetson());
        
    }

    private async void callBack_BoatListener(IAsyncResult result)
    {   
        if(_ecodroneBoatClienSocketListener.IsListening && !cts_boat.IsCancellationRequested)
        {
            try
            {
                HttpListener? listener = (HttpListener?)result.AsyncState;
                
                if(listener != null)
                {
                    HttpListenerContext context = listener.EndGetContext(result);
                    HttpListenerWebSocketContext websocket_context = await context.AcceptWebSocketAsync(null, new TimeSpan(1000));
                    
                    EcoClient? new_client = new EcoClient();

                    new_client.main_task = new Task (async () => { await HandlingClient(websocket_context.WebSocket, new_client); }, new_client.cts_client);
                    new_client.main_task.Start();

                    await Task.Run(() =>listener.BeginGetContext(callBack_BoatListener, listener), cts_boat);
                }
                
            }
            catch (ObjectDisposedException)
            {
                ///do nothing
                Debug.WriteLine("we manage this way object disposed exeption on close");
            }
            
        }else
        {
            
        }
    }

    public async Task HandlingClient(WebSocket webSocket, EcoClient? ecoClient) 
    {
        if(ecoClient != null)
        {
            while (webSocket.State == WebSocketState.Open && !ecoClient.cts_client.IsCancellationRequested)
            {
                if(ecoClient != null && ecoClient.IdClient != "NNN")
                {

                    //block is here
                    Tuple<string, Task> listen_task = Tuple.Create("listen_task", Task.Run(() => ecoClient.ReadWebSocket(webSocket, ecoClient, this)));

                    while(!listen_task.Item2.IsCompleted && !ecoClient.cts_client.IsCancellationRequested)
                    {
 
                        if(ecoClient.appState == ClientCommunicationStates.SENSORS_DATA || ecoClient.appState == ClientCommunicationStates.MISSIONS)
                        {
                            await ecoClient.TeensyChannelReadAndSend(this);
                        }
                    }
                    
                }else
                {
                    //here authenticate if not already 
                    ecoClient = await ReadFirstMessage(webSocket);

                    if(ecoClient.IdClient == "NNN")
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client is not created", CancellationToken.None);
                    }
                }
            }

            if(ecoClient.appState == ClientCommunicationStates.VIDEO)
            {
                EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                {
                    scope = 'U',
                    type = "0",
                    uuid = ecoClient.IdClient,
                    direction = "jetson_id",
                    identity = ecoClient.IdClient,
                    data = "NNN"
                };

                _videoBusService.Publish(ecodroneBoatMessage);
                _videoBusService.Unsubscribe(ecoClient.SerializeAndSendMessage, ecoClient.IdClient); 
            }

            //careful on aborted from client when server client is closed
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            _boatclients.Remove(ecoClient);

        }


    }

    
    public async Task<bool> DeactivateBoat()
    {

        teensySocketInstance.src_cts_teensy.Cancel();

        for (int i = 0; i < _boatclients.Count(); i++)
        {
                EcoClient ecoClient = _boatclients[i];

                ecoClient.src_cts_client.Cancel();
                if(ecoClient.main_task != null) 
                    ecoClient.main_task.Wait(ecoClient.cts_client);
        }
    

        src_cts_boat.Cancel();
        ecodroneVideo.src_cts_jetson.Cancel();
        
        if(ecodroneVideo.main_video_task != null)
            ecodroneVideo.main_video_task.Wait(ecodroneVideo.cts_jetson);


        _ecodroneBoatClienSocketListener.Stop();
        _ecodroneBoatClienSocketListener.Prefixes.Remove($"http://localhost:{port}/");
        _ecodroneBoatClienSocketListener.Close();

        ecodroneVideo._jetsonClientListener.Stop();
        ecodroneVideo._jetsonClientListener.Dispose();

        


        return true;
    }

    private async Task<EcoClient> ReadFirstMessage(WebSocket _webSocket)
    {
        EcoClient _client = new EcoClient()
        {
            IdClient = "NNN"
        };

        var buffer = new byte[1024 * 4];
        var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        byte message_scope = buffer[0];

        if(message_scope == 83)
        {
            _client.IdClient = Guid.NewGuid().ToString();
            _client._socketClient = _webSocket;

            _boatclients.Add(_client);

            //publish id for user
            EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
            {
                scope = 'U',
                type = "1",
                uuid = maskedId,
                direction = _client.IdClient,
                identity = _client.IdClient,
                data = null
            };

            string message_serialized = JsonConvert.SerializeObject(ecodroneBoatMessage);
            Debug.WriteLine(message_serialized);
            var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));

            await _client._socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);

        }else
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
        }

        return _client;
    }

    

    


}