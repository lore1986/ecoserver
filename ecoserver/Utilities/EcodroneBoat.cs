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
    public int port {get; set;} = 5058;

    public Task httpListener_task;
    public Task jetsonListener_task;
    public HttpListener _ecodroneBoatClienSocketListener;
    public EcodroneTeensyInstance teensySocketInstance {get; protected set;}
    public Dictionary<EcoClient, Task?> _boatclients = new Dictionary<EcoClient, Task?>();
    public VideoTcpListener ecodroneVideo {get; protected set;}
    public IVideoBusService _videoBusService { get; set; } = new VideoBusService();

    public CancellationTokenSource src_cts_boat = new CancellationTokenSource();
    public CancellationToken cts_boat {get; private set;}

    public Semaphore semaphore = new Semaphore(1,1);

    public CancellationTokenSource src_cts_block_listening = new CancellationTokenSource();
    public CancellationToken cts_block_listening;
    public EcodroneBoat(string _maskedid, byte[] sync, string IPT, int PT, bool setActive = false)
    {
        if(sync.Length != 3) { throw new ArgumentException("teensy sync not valid");}
        _Sync = sync;
        _IPTeensy = IPT;
        _PortTeensy = PT;
        isActive = setActive;
        maskedId = _maskedid;

        _ecodroneBoatClienSocketListener = new HttpListener();
        _ecodroneBoatClienSocketListener.Prefixes.Add($"http://*:{port}/interface/");// {_maskedid}/");
       
        cts_block_listening = src_cts_block_listening.Token;
       
        ecodroneVideo = new VideoTcpListener(_videoBusService, portvideo);
        teensySocketInstance = new EcodroneTeensyInstance(this, maskedId);
        
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
        _ecodroneBoatClienSocketListener.BeginGetContext(callBack_BoatListener, _ecodroneBoatClienSocketListener);
        ecodroneVideo._jetsonClientListener.BeginAcceptTcpClient(new AsyncCallback(ecodroneVideo.OnClientConnect), ecodroneVideo._jetsonClientListener);
    }

    private async void callBack_BoatListener(IAsyncResult result)
    {   
        
        try
        {
            if(_ecodroneBoatClienSocketListener.IsListening && !cts_boat.IsCancellationRequested)
            {
                HttpListener? listener = (HttpListener?)result.AsyncState;
                
                if(listener != null)
                {
                    HttpListenerContext context = listener.EndGetContext(result);
                    HttpListenerWebSocketContext websocket_context = await context.AcceptWebSocketAsync(null, new TimeSpan(1000));
                    
                    EcoClient new_client = new EcoClient(this);
                    Task client_run = Task.Run(async () => { await HandlingClient(websocket_context.WebSocket, new_client); });

                    if(_boatclients.Count() == 0)
                    {
                        RestartTeensyTalkSignal();
                    }

                    _boatclients.Add(new_client, client_run);

                    await Task.Run(() =>listener.BeginGetContext(callBack_BoatListener, listener), cts_boat);
                }
            }
            
        }
        catch (ObjectDisposedException)
        {
            ///do nothing
            Debug.WriteLine("we manage this way object disposed exeption on close");
        }
            
        
    }

    public void RestartTeensyTalkSignal()
    {
        teensySocketInstance.src_cts_teensy = new CancellationTokenSource();
        teensySocketInstance.cts_teensy = teensySocketInstance.src_cts_teensy.Token;
        Task.Run(teensySocketInstance.StartTeensyTalk, teensySocketInstance.cts_teensy);
    }

    public async Task HandlingClient(WebSocket webSocket, EcoClient ecoClient) 
    {
        while (!cts_block_listening.IsCancellationRequested)
        {
            ecoClient = await ReadFirstMessage(webSocket, ecoClient);
            teensySocketInstance.signalBusSocket.Subscribe(ecoClient.TeensyReadAndSend, ecoClient);

            while (webSocket.State == WebSocketState.Open && !cts_block_listening.IsCancellationRequested)
            {
                await Task.Run(() => ecoClient.ReadWebSocket(webSocket, this));
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
            _boatclients.Remove(ecoClient);
        }

        

            

    }

    
    private async Task<EcoClient> ReadFirstMessage(WebSocket _webSocket, EcoClient _client)
    {
       
        var buffer = new byte[1024 * 4];
        var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        byte message_scope = buffer[0];

        if(message_scope == 83)
        {
            _client.IdClient = Guid.NewGuid().ToString();
            _client._socketClient = _webSocket;
            
        
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