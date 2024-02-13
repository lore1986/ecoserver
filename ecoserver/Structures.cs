using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using webapi.Services.BusService;
using webapi.Services.SocketService;


namespace webapi
{

    struct TeensyMessage
    {
        public byte lengCmd = 0;
        public byte sorgCmd = 0;
        public byte destCmd = 0;
        public byte id_dCmd = 0;
        public byte Cmd1 = 0;
        public byte Cmd2 = 0;
        public byte Cmd3 = 0;

        public TeensyMessage(byte[] data)
        {
            this.lengCmd = data[0];
            this.sorgCmd = data[1];
            this.destCmd = data[2];
            this.id_dCmd = data[3];
            this.Cmd1 = data[4];
            this.Cmd2 = data[5];
            this.Cmd3 = data[6];
        }
    }

    struct ParamId
    {
        public byte[] Boat;

        // Constructor to initialize the Boat array
        public ParamId()
        {
            Boat = new byte[] { 0x10, 0x11, 0x12 };
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    class  ImuData
    {
        public float Yaw { get; set; } = 0;
        public float Pitch { get; set; } = 0;
        public float Roll { get; set; } = 0;
        public float Ax { get; set; } = 0;
        public float Ay { get; set; } = 0;
        public float Az { get; set; } = 0;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MissionParamIn
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] idMission; //Mission ID
        public UInt16 nMission; //Mission number
        public UInt16 total_mission_nWP; //Number of waypoints of current mission
        public UInt16 wpStart;//Start waypoint of repetition cycle
        public UInt16 cycles; //How many execution cycles of the current mission
        public UInt16 wpEnd;  //Final waypoint of repetition cycle
        public byte NMmode; //What to do next (0,1,2,3,4) - 0: nessuna missione; 1: da fine cicla vai alla successiva;
                        //  2: dopo tutti i waypoint vai alla successiva; 3: all'ultimo WP del ciclo stazioni in un certo raggio fino a nuovo ordine
                        // 4: staziona all'ultimo WP della missione
        public UInt16 NMnum;  //Number of the next mission
        public UInt16 NMStartInd; //Start waypoint of next mission
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] idMissionNext; //String with the path of the mission
        public float standRadius; //WP standing mode radius
    }


    public class MissionParam
    {
        public string idMission = ""; //Mission ID char idMission[32]; //Mission ID
        public UInt16 nMission; //Mission number
        public UInt16 total_mission_nWP; //Number of waypoints of current mission
        public UInt16 wpStart;//Start waypoint of repetition cycle
        public UInt16 cycles; //How many execution cycles of the current mission
        public UInt16 wpEnd;  //Final waypoint of repetition cycle
        public byte NMmode; //What to do next (0,1,2,3,4) - 0: nessuna missione; 1: da fine cicla vai alla successiva;
                              //  2: dopo tutti i waypoint vai alla successiva; 3: all'ultimo WP del ciclo stazioni in un certo raggio fino a nuovo ordine
                              // 4: staziona all'ultimo WP della missione
        public UInt16 NMnum;  //Number of the next mission
        public UInt16 NMStartInd; //Start waypoint of next mission

        public string idMissionNext = ""; //String with the path of the mission char idMission[32]; //Mission ID
        public float standRadius; //WP standing mode radius 
    }



    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WayPoint
    {
        public UInt16 Nmissione;// Nome del file contenente il waypoint 
        public UInt16 IndexWP;// Indice dell'attuale waypoint
        public float Latitude;// Latitudine attuale WP
        public float Longitude;// Latitudine attuale WP
        public byte NavMode;// [0 - 9]: definita come velocità eprm base all'indice [NavMode] dell'array di 10 velocità -> modifica erpm_base dei motori
                              //************* // [10 - 19]: definita come velocità gps all'indice [NavMode] dell'array di 10 velocità -> modifica rif. di velocità gps
                              //************* // [20 - 29]: definita come compensazione di energia all'indice [NavMode] dell'array di 10 livelli -> modifica erpm_max/min dei motori
        public byte PointType; // 0: fermo; 1: non fermo; 2: fa parte di un'area; 3: stazionamento in un raggio
        public byte MonitoringOp;//[1 - 125]: ti fermi ed effettui monitoraggio; 126 - 255: non ti fermi ed effettui monitoraggio; 0: non ti fermi e vai avanti
        public byte ArriveMode;// [0 - 49]: senza vincoli intermedi
                                 //*****************// [50 - 99]: algoritmo punti intermedi con retta cartesiana
                                 //*****************// [100 - 149]: algoritmo punti intermedi con emisenoverso
        public float WaypointRadius; //Raggio entro il quale passare al WP successivo
                              //16.600.000 waypoint totali sulla flash
    }


    public class EcodroneUsers
    {
        public int Id { get; }
        public string Identification { get; set; } = "NNN";
        public string Password { get; set; } = "NNN";
        public DateTime LastLogin { get; set; } = DateTime.Now;

        public EcodroneUsers(int id)
        {
            Id = id;
        }

        public bool ReturnHashedPassword(string password)
        {
            //Add Hashing and Encrypt for password
            if (password == null || password.Length == 0) { return false; }
            if (password == Password) { return true; }

            return false;
        }

    }

    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }


    /*public class CustomerGroup
    {
        public string GroupId { get; private set; }
        public List<TeensyGroup> teensyGroups;
        public CustomerGroup(string customerGroupName)
        {
            GroupId = customerGroupName;
            teensyGroups = new List<TeensyGroup>();
        }
    }*/

    public class EcodroneBoat
    {
        public string MaskedId { get; set; } = "NNN";
        public byte[] _Sync { get; private set; }
        public string _IPTeensy { get; private set; }
        public int _PortTeensy { get; private set; }
        public bool isActive { get; protected set; } = false;

        public EcodroneBoat(byte[] sync, string IPT, int PT, bool setActive = false)
        {
            if(sync.Length != 3) { throw new ArgumentException("teensy sync not valid");}
            _Sync = sync;
            _IPTeensy = IPT;
            _PortTeensy = PT;
            isActive = setActive;
        }

        public EcodroneBoat ChangeState(bool state)
        {
            this.isActive = state;
            return this;
        }
    }

    public class TeensySocketInstance
    {
        public EcodroneBoat ecodroneBoat { get; private set; }
        public TcpClient? TcpSocket { get; set; }
        public NetworkStream? NetworkStream { get; set; }

        public Task? TaskReading { get; set; } = null;

        public List<MessageContainerClass> task_que;
        public List<MessageContainerClass> command_task_que;
        public ITeensyMessageConstructParser _teensyLibParser { get; private set; }
        public Channel<ChannelTeensyMessage> channelTeensy { get; set; }

        //public JetsonSocketHandler jetsonSocket {  get; set; }

        public TeensySocketInstance(EcodroneBoat boat, string gid)
        { 
            ecodroneBoat = boat;
            ecodroneBoat.MaskedId = gid;  //change this one for now ok 
            task_que = new List<MessageContainerClass>();
            _teensyLibParser =  new TeensyMessageConstructParser();
            command_task_que = new List<MessageContainerClass>();
            channelTeensy = Channel.CreateUnbounded<ChannelTeensyMessage>(new UnboundedChannelOptions
            {
                SingleWriter = false,
                SingleReader = false,
                AllowSynchronousContinuations = true
            });
            //jetsonSocket = new JetsonSocketHandler();
        }
    }

    public class EcoClient
    {
        public string IdClient { get; set; } = "NNN";
        public GroupRole groupRole { get; set; } = GroupRole.None;

        public WebSocket? socketMessageVideo { get; set; } = null;

        /*public EcoClient(TcpClient webSocket)
        {
            MidSocket = webSocket;
        }*/
    }

    public enum GroupRole
    {
        None,
        Admin
    }

  

    public class ChannelTeensyMessage
    {
        //data parsed
        public byte[]? data_in { get; set; } = null;
        //data when command needs ping pong
        public byte[]? data_command { get; set; } = null;
        //needs ping pong? just confirm it's a double check
        public bool NeedAnswer { get; set; } = false;

        //basically if data_command == null it Need Preparation (but not always i think need to check)
        public bool NeedPreparation { get; set; } = false; 
      
    }

    public class BusEventMessage : EventArgs
    {
        public string idTeensy { get; set; }
        public byte[] data { get; set; }

        //sender
        public BusEventMessage(string idT, byte[] data)
        {
            idTeensy = idT;
            this.data = data;
        }
    }

    public class WayPointEventArgs : EventArgs
    {
        public List<WayPoint> WayPoints { get; private set; }
        public WayPointEventArgs(List<WayPoint> inways)
        {
            WayPoints = inways;
        }
    }

    /*public class VideoMessageClient
    {
        public string scope { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string data { get; set; } = string.Empty;
        public string identity { get; set; } = string.Empty;

        public VideoMessageClient TransformVideoMessageToClient(VideoMessageServer videoMessageServer)
        {
            return new VideoMessageClient()
            {
                scope = videoMessageServer.scope,
                type = videoMessageServer.type,
                data = videoMessageServer.data,
                identity = videoMessageServer.identity
            };
        }
    }*/
    /*public class VideoMessageListeners : 
    {
        public string byte_id { get; set; } = "NNN";
        public Task<VideoMessage>? ReadAction { get; set; } = null;
    }*/

    public class VideoMessage : EventArgs
    {
        public char scope { get; set; } = 'N';
        public string type { get; set; } = string.Empty;
        public string uuid { get; set; } = string.Empty;
        public string direction { get; set; } = string.Empty;
        public object? data { get; set; }
        public string identity { get; set; } = string.Empty;

        public VideoMessage TransformVideoMessageToServer(VideoMessage videoMessageClient, string uuidto, string directionto)
        {
            return new VideoMessage()
            {
                scope = videoMessageClient.scope,
                type = videoMessageClient.type,
                identity = videoMessageClient.identity,
                direction = directionto,
                uuid = uuidto
            };
        }
    }


    /*public class StandardMessage : VideoMessage
    {
        public string data { get; set; } = string.Empty;
    }

    public class ProtocolSdpMessage : VideoMessage
    {
        public SdpMessage data { get; set; } = new SdpMessage();
    }
    public class ProtocolIceMessage : VideoMessage
    {
        public IceMessage data { get; set; } = new IceMessage();
    }

    public class SdpMessage 
    {
        public string type { get; set; } = string.Empty;
        public string sdp { get; set; } = string.Empty;
    }

    public class IceMessage 
    {
        *//*public string sdpMLineIndex { get; set; } = string.Empty;
        public string candidate { get; set; } = string.Empty;*//*
        public string candidate { get; set; } = string.Empty;
        [JsonProperty("sdpMid")]
        public string? SdpMid { get; set; }
        [JsonProperty("sdpMLineIndex")]
        public int? SdpMLineIndex { get; set; }
        [JsonProperty("usernameFragment")]
        public string? UsernameFragment { get; set; }
    }*/


    public class NewClientEventArgs : EventArgs
    {
        public string maskedTeensyId { get; set; } = "NNN";
        public string userid { get; set; }

    }

/*    public class VideoBusEventMessage : EventArgs
    {
        public string group_id { get; set; } = "NNN";
        public VideoMessage videoMessageServer { get; set; } = new VideoMessage();
    }*/

    

    public class VideoListener : IDisposable
    {
        public string uuid { get; set; } = string.Empty;
        public TcpClient? sock_et { get; set; } = null;
        public NetworkStream? networkStream { get; set; } = null;
        public IVideoBusService? listener_videoBus { get; private set; }

        public void Dispose()
        {
            networkStream?.Dispose();
            sock_et?.Dispose();
        }

        public void SetVideoBus(IVideoBusService? videoBus)
        {
            if(videoBus == null) throw new ArgumentNullException(nameof(videoBus));
            listener_videoBus = videoBus;
        }
    }

    public class VideoClientListener : VideoListener
    {
        public WebSocket? _websocket { get; set; } = null;

         public async void ReadAndSendClient(VideoMessage v)
        {

            if (v.direction == uuid)
            {
                try 
                {
                    if (_websocket != null)
                    {
                        string message_out = JsonSerializer.Serialize(v);
                        byte[] message = Encoding.UTF8.GetBytes(message_out);
                        await _websocket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        //public NetworkStream? networkStream { get; set; } = null;

        // public void Dispose()
        // {
        //     networkStream?.Dispose();
        //     sock_et?.Dispose();
        // }

    }

   
}
