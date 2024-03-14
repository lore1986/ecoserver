using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
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

    public List<Task> client_listen_task = new List<Task>();


    //public TaskFactory taskFactory;


    public CancellationTokenSource cts_all = new CancellationTokenSource();
    public CancellationToken cancellationToken {get; private set;}

    public CancellationTokenSource cts_client = new CancellationTokenSource();
    public CancellationToken client_cancellationToken {get; private set;}

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
        
        //taskFactory = new TaskFactory(cancellationToken);
        cancellationToken = cts_all.Token;
        client_cancellationToken = cts_client.Token;
    }

    public EcodroneBoat ChangeState(bool state)
    {
        isActive = state;
        return this;
    }

    private bool IsCorrectByte(byte byte_zero)
    {
        List<byte> bytes_admitted = new List<byte>
        {
            83,
            67,
            86
        };
        
        return bytes_admitted.Contains(byte_zero);
    }
    

    public void StartEcodroneBoatTasks()
    {
        _ecodroneBoatClienSocketListener.Start();

        teensySocketInstance.TaskReading = Task.Run(() => teensySocketInstance.StartTeensyTalk(cancellationToken), cancellationToken); //=> teensySocketInstance.StartTeensyTalk(), _ecodroneBoat.cancellationToken);
        
        httpListener_task = Task.Run(() => callBack_BoatListener(), cancellationToken);
        jetsonListener_task = Task.Run(() => ecodroneVideo.ListenJetson(cancellationToken), cancellationToken);

        

    }

    
    private async Task callBack_BoatListener()
    {
        
        while (!cancellationToken.IsCancellationRequested)
        {
            
            HttpListenerContext _client_context = await _ecodroneBoatClienSocketListener.GetContextAsync();
            Debug.WriteLine($"boat {maskedId} listener is listening for receiver of teensy data ");

            client_listen_task.Add(Task.Run(async () => {
                
                HttpListenerWebSocketContext websocket_context = await _client_context.AcceptWebSocketAsync(null);  //websocket_context is a big object
                Debug.WriteLine($"boat {maskedId} listener got client {websocket_context.User} connected.");

                await HandlingClient(websocket_context.WebSocket);

            }, cancellationToken));

        }

        Debug.WriteLine("i am called");
        _ecodroneBoatClienSocketListener.Stop();
        _ecodroneBoatClienSocketListener.Close();

        
                
    }

    public async Task ReadWebSocket(WebSocket _webSocket, EcoClient? ecoClient)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        byte message_scope = buffer[0];
    
        if(IsCorrectByte(message_scope))
        {
            
            byte[] message_length = new byte[4];
            byte[] byte_message = new byte[receiveResult.Count - 4];
            
            Array.Copy(buffer, 1, message_length, 0, message_length.Length);
            Array.Copy(buffer, 5, byte_message, 0, byte_message.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(message_length); // Reverse the byte array if the system is little-endian
            }
            
            EcodroneBoatMessage? message = JsonConvert.DeserializeObject<EcodroneBoatMessage>(Encoding.UTF8.GetString(byte_message));

            switch (message_scope)
            {
                case 67 : //C
                {
                    //command
                    if(message != null)
                    {
                        if(message.data != null)
                        {
                            GotCommand((byte[])message.data);  //fix command message data 
                        }
                    }                
                }
                break;
                case 86 : //V
                {
                    if(ecoClient != null)
                    {
                        if(message != null)
                        {
                            ecoClient.VideoCommunication(message, this);
                        }   
                    }
                }
                break;
                case 83: //S
                {
                    if(ecoClient != null)
                    {
                                                
                        if(message != null)
                        {
                            ecoClient.BoatClientAppStateManager(message, this);
                        }   
                    }
                    

                }
                break;
                default:
                //store in variable or discard if message is not full
                {
                    // if(_ecoClient != null)
                    // {
                    //     await TeensyChannelReadAndSend(_ecoClient);
                    // }
                }
                break;
            }
        }
        
    }


    


    private int CopyToByteArray(byte[] source, byte[] destination, int startIndex)
    {
        Array.Copy(source, 0, destination, startIndex, source.Length);
        return startIndex + source.Length;
    }

    

    
    private void GotCommand(byte[] data)
    {
        _ = new cmdRW();

        if (data[5] == cmdRW.SAVE_MISSION_PARAM_CMD3 && data[4] == cmdRW.SAVE_MISSION_CMD2 && data[3] == cmdRW.REQUEST_CMD1)
        {
            byte[] command_array = data.Take(6).ToArray();

            var receivedMessage = Encoding.UTF8.GetString(data.Skip(6).ToArray());

            MissionDataPayload? payload = JsonConvert.DeserializeObject<MissionDataPayload>(receivedMessage);

            if (payload != null)
            {

                List<WayPoint> waypoints = new List<WayPoint>();
                MissionParam param = new MissionParam
                {
                    idMission = payload.MissionParam.IdMission + '\0',
                    nMission = payload.MissionParam.MissionNumber,
                    total_mission_nWP = payload.MissionParam.TotalWayPoint,
                    wpStart = payload.MissionParam.WpStart,
                    cycles = payload.MissionParam.Cycles,
                    wpEnd = payload.MissionParam.WpEnd,
                    NMmode = (byte)payload.MissionParam.NMmode,
                    NMnum = payload.MissionParam.NMnum,
                    NMStartInd = payload.MissionParam.NMstart,
                    idMissionNext = "ARP12/21" + '\0',
                    standRadius = payload.MissionParam.StandRadius
                };


                byte[] arridMission = new byte[32];
                byte[] id_miss = Encoding.UTF8.GetBytes(param.idMission);
                for (int i = 0; i < id_miss.Length; i++)
                {
                    arridMission[i] = id_miss[i];
                }



                byte[] arridMissionNext = new byte[32];
                byte[] idMissNext = Encoding.UTF8.GetBytes(param.idMissionNext);
                for (int i = 0; i < idMissNext.Length; i++)
                {
                    arridMissionNext[i] = idMissNext[i];
                }
                
                //_logger.LogInformation(arridMission.Length.ToString());


                byte[] payload_header_mission = new byte[19 + arridMission.Length + arridMissionNext.Length]; //(32 * 2)

                var index = 0;

                index = CopyToByteArray(arridMission, payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.nMission), payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.total_mission_nWP), payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.wpStart), payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.cycles), payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.wpEnd), payload_header_mission, index);

                byte[] nmModeB = new byte[1];
                nmModeB[0] = param.NMmode;
                index = CopyToByteArray(nmModeB, payload_header_mission, index);



                index = CopyToByteArray(BitConverter.GetBytes(param.NMnum), payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.NMStartInd), payload_header_mission, index);
                index = CopyToByteArray(arridMissionNext, payload_header_mission, index);
                index = CopyToByteArray(BitConverter.GetBytes(param.standRadius), payload_header_mission, index);


                if (param.wpStart != 0)
                {
                    List<Point> reordered_list = new List<Point>();

                    int index_i = param.wpStart;


                    if (payload.PointsList == null)
                    {
                        return;
                    }
                    int original_count = payload.PointsList.Count;

                    while (payload.PointsList.Count > 0)
                    {
                        if (index > original_count)
                        {
                            index = 0;
                        }

                        reordered_list.Add(payload.PointsList[index]);
                        payload.PointsList.RemoveAt(index);
                    }

                    payload.PointsList = reordered_list;
                }


                byte[] combinedArray = command_array.Concat(payload_header_mission).ToArray();


                for (int i = 0; i < payload.PointsList.Count; i++)
                {
                    WayPoint wayPoint = new WayPoint
                    {
                        Longitude = (float)payload.PointsList[i].Lng,
                        Latitude = (float)payload.PointsList[i].Lat,
                        ArriveMode = (byte)payload.PointsList[i].Amode,
                        WaypointRadius = payload.PointsList[i].Wrad,
                        IndexWP = (byte)i,
                        MonitoringOp = (byte)payload.PointsList[i].Mon,
                        NavMode = (byte)payload.PointsList[i].Navmode,
                        PointType = (byte)payload.PointsList[i].PointType,
                        Nmissione = param.nMission
                    };

                    waypoints.Add(wayPoint);
                }

                WayPointEventArgs wayPointEventArgs = new WayPointEventArgs(waypoints);

                teensySocketInstance._teensyLibParser.UpdateListWayPoints(waypoints);

                teensySocketInstance.command_task_que.Add(
                    new TeensyMessageContainer("new_user_command", combinedArray, needPreparation: true)
                );

            }
        }
        else
        {
            teensySocketInstance.command_task_que.Add(
            new TeensyMessageContainer("new_user_command", data, needPreparation: true));
            
        }

    }        
    
    public async Task<bool> DeactivateBoat()
    {
        cts_client.Cancel();
        cts_all.Cancel();
        
        // if(teensySocketInstance != null && teensySocketInstance.TaskReading != null)
        // {
        //    await teensySocketInstance.TaskReading.WaitAsync(cancellationToken);
        // }
        
        
        //httpListener_task.Wait(cancellationToken);
        
        // _ecodroneBoatClienSocketListener.Stop();
        // _ecodroneBoatClienSocketListener.Close();

        // if(ecodroneVideo.jetson_server != null && ecodroneVideo.jetson_server.sock_et != null)
        // {
        //     ecodroneVideo._jetsonClientListener.Stop();
        //     ecodroneVideo.jetson_server.sock_et.Close();
        //     ecodroneVideo.jetson_server.sock_et.Dispose();
        //     //ecodroneVideo._jetsonClientListener.Server.Shutdown(System.Net.Sockets.SocketShutdown.Both);
        // }


        //;
        //await jetsonListener_task.WaitAsync(cancellationToken);

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
        //string receivedMessage = string.Empty;

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

    public async Task HandlingClient(WebSocket webSocket) 
    {

 
        EcoClient? ecoClient = null;
        
        while (!client_cancellationToken.IsCancellationRequested)
        {
            if(ecoClient != null)
            {
                Task boat_racing_tasks = ReadWebSocket(webSocket, ecoClient);
                
                while(!boat_racing_tasks.IsCompleted && !client_cancellationToken.IsCancellationRequested)
                {
                    if(ecoClient.appState == ClientCommunicationStates.VIDEO)
                    {
                        if(!_videoBusService.IsASubscriber(ecoClient.IdClient))
                        {
                            _videoBusService.Subscribe(ecoClient.SerializeAndSendMessage, ecoClient.IdClient);
                        }
                    }

                    
                    if(ecoClient.appState == ClientCommunicationStates.SENSORS_DATA)
                    {
                        
                            await TeensyChannelReadAndSendData(ecoClient);
                        
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
        

        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
        _boatclients.Remove(ecoClient);
        

    
    }

    private async Task TeensyChannelReadAndSendData(EcoClient ecoClient)
    {
        ChannelTeensyMessage? messageTeensy = teensySocketInstance.ReadOnChannel();

        if (messageTeensy != null)
        {
            if (messageTeensy.data_message != "NNN")
            {
                if(ecoClient != null)
                {
                    if(ecoClient._socketClient != null  && ecoClient._socketClient.State == WebSocketState.Open)
                    {
                        //Debug.WriteLine($"message is {messageTeensy.data_message}" );
                        
                        EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                        {
                            scope = 'D',
                            type = "1", 
                            uuid = maskedId,
                            direction = ecoClient.IdClient,
                            identity = messageTeensy.message_id,
                            data = messageTeensy.data_message
                        };

                        string message_serialized = JsonConvert.SerializeObject(ecodroneBoatMessage, Formatting.Indented);
                        
                        Debug.WriteLine("message serialized teensy ", message_serialized);
                        
                        var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));

                        //await Task.Delay(500);
                        await ecoClient._socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            
            }

        }
    }


}