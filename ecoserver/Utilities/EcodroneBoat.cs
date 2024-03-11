using System.Diagnostics;
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
    public EcodroneTeensyInstance teensySocketInstance {get; protected set;}
    public List<EcoClient> _boatclients = new List<EcoClient>();
    public VideoTcpListener ecodroneVideo {get; protected set;}
    public IVideoBusService _videoBusService { get; set; } = new VideoBusService();

    public EcodroneBoat(string _maskedid, byte[] sync, string IPT, int PT, bool setActive = false)
    {
        if(sync.Length != 3) { throw new ArgumentException("teensy sync not valid");}
        _Sync = sync;
        _IPTeensy = IPT;
        _PortTeensy = PT;
        isActive = setActive;
        maskedId = _maskedid;

        teensySocketInstance = new EcodroneTeensyInstance(this, _maskedid);
        ecodroneVideo = new VideoTcpListener(_videoBusService, portvideo);
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
            // /83,
            67,
            86
        };
        
        return bytes_admitted.Contains(byte_zero);
    }
    

    public async Task ReadWebSocket(WebSocket _webSocket, EcoClient? ecoClient)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //string receivedMessage = string.Empty;

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
                        ecoClient.clientCommunicationScope = ecoClient.clientCommunicationScope != ClientCommunicationScopes.VIDEO ? ClientCommunicationScopes.VIDEO : ecoClient.clientCommunicationScope;
                        
                        if(message != null)
                        {
                            VideoCommunication(message, ecoClient);
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

    public void VideoCommunication(EcodroneBoatMessage videoMessage, EcoClient? ecoClient)
    {
        if (videoMessage != null && ecoClient != null)
        {
            switch (videoMessage.scope)
            {
                case 'U':
                {
                    EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                    {
                        scope = 'U',
                        type = "1",
                        uuid = "jetson_id",
                        identity = ecoClient.IdClient,
                        data = "data",
                        direction = ecoClient.IdClient,
                    };

                    ecoClient.SerializeAndSendMessage(ecodroneBoatMessage);

                    ecodroneBoatMessage.direction = "jetson_id";
                    ecodroneBoatMessage.uuid = ecoClient.IdClient;

                    _videoBusService.Publish(ecodroneBoatMessage);
                }
                break;
                default:
                {
                    if(videoMessage.data != null)
                    {
                        videoMessage.data = videoMessage.data.ToString();
                        _videoBusService.Publish(videoMessage);
                    }
                    
                }
                break;
            }
        }

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
    

}