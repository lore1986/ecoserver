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

    // public Task httpListener_task;
    // public Task jetsonListener_task;
    public HttpListener _ecodroneBoatClienSocketListener;
    public EcodroneTeensyInstance teensySocketInstance {get; protected set;}
    public List<EcoClient> _boatclients = new List<EcoClient>();
    public VideoTcpListener ecodroneVideo {get; protected set;}
    public IVideoBusService _videoBusService { get; set; } = new VideoBusService();
    public ISignalBusSocket signalBusSocket;
    public ITeensyMessageConstructParser _teensyLibParser { get; private set; }
    public readonly EcodroneMessagesContainers ecodroneMessagesContainers;

    public CancellationTokenSource src_cts_boat = new CancellationTokenSource();
    public CancellationToken cts_boat {get; private set;}
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
        
        signalBusSocket = new SignalBusSocket(this);
        _teensyLibParser = new TeensyMessageConstructParser(this);
        ecodroneMessagesContainers = new EcodroneMessagesContainers(_teensyLibParser);

        _ecodroneBoatClienSocketListener = new HttpListener();
        _ecodroneBoatClienSocketListener.Prefixes.Add($"http://localhost:{port}/interface/");// {_maskedid}/");
       
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
                    
                    var buffer = new byte[1024 * 4];
                    await websocket_context.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    byte message_scope = buffer.First();

                    if(message_scope == 83)
                    {
                        EcoClient new_client = new EcoClient(this, websocket_context.WebSocket);
                        _boatclients.Add(new_client);
                    }else
                    {
                        await websocket_context.WebSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, null, CancellationToken.None);
                        websocket_context.WebSocket.Abort();
                        websocket_context.WebSocket.Dispose();
                    }
                    
                    //this is good but fix it
                    // if(_boatclients.Count() == 0)
                    // {
                    //     RestartTeensyTalkSignal();
                    // }

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

    

    

    

    


}