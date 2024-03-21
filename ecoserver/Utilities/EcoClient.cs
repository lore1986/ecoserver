using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using webapi;
using webapi.Utilities;

public enum EcoClientRole
{
    None,
    Admin
}

public class EcoClient
{
    public string IdClient { get; set; } = "NNN";
    public WebSocket? _socketClient {get; set;}
    public ClientCommunicationStates appState = ClientCommunicationStates.SENSORS_DATA;

    public CancellationTokenSource src_cts_client = new CancellationTokenSource();
    public CancellationToken cts_client {get; private set;}

    public Task? main_task {get; set;}
    public Task? taskina {get; set;}
    public Task? taskone {get; set;}
    public List<Tuple<string, Task>> client_listen_task = new List<Tuple<string, Task>>();

    public EcoClientRole ecoClientRole = EcoClientRole.Admin;
    // public CancellationTokenSource src_cts_block_listening;
    // public CancellationToken cts_block_listening = new CancellationToken();

    public bool isListening = true;
    public EcoClient(EcodroneTeensyInstance ecodroneTeensyInstance)
    {
        cts_client = src_cts_client.Token;
        //src_cts_block_listening = CancellationTokenSource.CreateLinkedTokenSource(cts_block_listening);
        ecodroneTeensyInstance.signalBusSocket.Subscribe(TeensyChannelReadAndSend, IdClient);
    }

    public void TeensyChannelReadAndSend(ChannelTeensyMessage messageTeensy)
    {
        
        //ABSOLUTELY FIND A WAY TO DO TO AVOID MESSAGGING CLIENT WITHOUT SENSE
        // && messageTeensy.id_client != null && messageTeensy.id_client == IdClient

        if (messageTeensy != null)
        {
            if (messageTeensy.data_message != "NNN")
            {
                if(_socketClient != null  && _socketClient.State == WebSocketState.Open)
                {
                    EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage();

                    switch (appState)
                    {
                        case ClientCommunicationStates.SENSORS_DATA:
                        {
                            if(appState == EcodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id))
                            {
                                ecodroneBoatMessage.scope = 'D';
                                ecodroneBoatMessage.type = "1"; 
                            }else
                            {
                                return;
                            }
                            
                        }
                        break;
                        case ClientCommunicationStates.MISSIONS:
                        {
                            if(appState == EcodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id)) 
                            {
                                switch (messageTeensy.message_id)
                                {
                                    case "DTree":
                                        ecodroneBoatMessage.scope = 'M';
                                        ecodroneBoatMessage.type = "0";
                                        break;
                                    case "MMW":
                                        ecodroneBoatMessage.scope = 'M';
                                        ecodroneBoatMessage.type = "1";
                                        break;  
                                    case "AllWayPoints":
                                        ecodroneBoatMessage.scope = 'M';
                                        ecodroneBoatMessage.type = "2";
                                        break;  
                                    default:
                                        break;
                                }
                                
                            }else
                            {
                                return;
                            }
                        }
                        break;
                        case ClientCommunicationStates.WAYPOINT:
                        {
                            if(appState == EcodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id)) // && messageTeensy.id_client != null && messageTeensy.id_client == IdClient
                            {
                                ecodroneBoatMessage.scope = 'W';
                                ecodroneBoatMessage.type = "1";
                                
                            }else
                            {
                                return;
                            }
                        }
                        break;
                        default:
                        {
                            ecodroneBoatMessage.scope = '3';
                            ecodroneBoatMessage.type = "0"; 
                            return;
                        }
                    }

                    
                    ecodroneBoatMessage.uuid = "ecodrone_boatone";
                    ecodroneBoatMessage.direction = IdClient;
                    ecodroneBoatMessage.identity = messageTeensy.message_id;
                    ecodroneBoatMessage.data = messageTeensy.data_message;

                    string message_serialized = JsonConvert.SerializeObject(ecodroneBoatMessage, Formatting.Indented); 
                    //Debug.WriteLine("message serialized teensy ", message_serialized);
                    var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));
                    _socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
                    
                }

            
            }

        }
    }

    public async void SerializeAndSendMessage(EcodroneBoatMessage ecodroneMessage)
    {
        if(_socketClient != null)
        {
            if (ecodroneMessage.direction == IdClient)
            {
                if(ecodroneMessage.scope == 'M' && ecodroneMessage.type == "1")
                {
                    Debug.WriteLine("check this");
                }
                string message_serialized = JsonConvert.SerializeObject(ecodroneMessage, Formatting.Indented);
                           
                var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));

                await _socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            
        }
    }
    public async void BoatClientAppStateManager(EcodroneBoatMessage stateMessage, EcodroneBoat boat)
    {
        if (stateMessage != null)
        {
            
            int message_type = short.Parse(stateMessage.type);

            switch(message_type)
            {
                case 0:
                {
                    //unload everything on client close window to implement
                    if(_socketClient != null)
                    {
                        if(_socketClient.State == WebSocketState.Open)
                        {
                            await _socketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, $"closing {IdClient} for unload", CancellationToken.None);
                        }

                        //useroff
                        
                    }
                    
                }
                break;
                case 1:
                {
                    switch(stateMessage.identity)
                    {
                        case "STD":
                        {
                            if(appState != ClientCommunicationStates.SENSORS_DATA)
                            {

                                if(appState == ClientCommunicationStates.VIDEO)
                                {
                                    UnsubscribeVideo(boat);
                                }

                                appState = ClientCommunicationStates.SENSORS_DATA;
                            }
                        }
                        break;
                        case "MSS":
                        {
                            if(appState != ClientCommunicationStates.MISSIONS)
                            {
                                if(appState == ClientCommunicationStates.VIDEO)
                                {
                                    UnsubscribeVideo(boat);
                                }
                                appState = ClientCommunicationStates.MISSIONS;
                            }
                        }
                        break;
                        case "VID":
                        {
                            if(appState != ClientCommunicationStates.VIDEO)
                            {
                                boat._videoBusService.Subscribe(SerializeAndSendMessage, IdClient);
                                appState = ClientCommunicationStates.VIDEO;
                            }
                        }
                        break;
                        case "WPY":
                        {
                            if(appState == ClientCommunicationStates.VIDEO)
                            {
                                UnsubscribeVideo(boat);
                            }
                            appState = ClientCommunicationStates.WAYPOINT;
                        }
                        break;
                    }
                }
                break;
            }
            
        }
    }

    private void UnsubscribeVideo(EcodroneBoat _boat)
    {
        EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
        {
            scope = 'U',
            type = "0",
            uuid = IdClient,
            identity = IdClient,
            data = "data",
            direction = "jetson_id",
        };
        
        
        _boat._videoBusService.Publish(ecodroneBoatMessage);
        _boat._videoBusService.Unsubscribe(SerializeAndSendMessage, IdClient);
    }
    public void VideoCommunication(EcodroneBoatMessage videoMessage, EcodroneBoat boat)
    {
        if (videoMessage != null)
        {
            switch (videoMessage.scope)
            {
                case 'U':
                {
                    int message_type = short.Parse(videoMessage.type);

                    switch(message_type)
                    {
                        case 1:
                        {
                            EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                            {
                                scope = 'U',
                                type = "1",
                                uuid = IdClient,
                                identity = "boat",
                                data = "data",
                                direction = "jetson_id",
                            };


                            boat._videoBusService.Publish(ecodroneBoatMessage);
                        }
                        break;
                        case 0:
                        {
                            if(videoMessage.data != null)
                            {
                                videoMessage.data = videoMessage.data.ToString();
                                boat._videoBusService.Publish(videoMessage);
                            }
                        }
                        break;
                    }
                    
                }
                break;
                default:
                {
                    if(videoMessage.data != null)
                    {
                        videoMessage.data = videoMessage.data.ToString();
                        boat._videoBusService.Publish(videoMessage);
                    }
                    
                }
                break;
            }
        }

    }

    public async Task ReadWebSocket(WebSocket _webSocket, EcodroneBoat boat)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        byte message_scope = buffer[0];
    
        if(boat.IsCorrectByte(message_scope))
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
                case 72: //W
                {
                    if(message.data != null)
                    {
                        // VERIFY ID CLIENT TO IMPLEMENT
                        //UPLOADING MISSIONS IS A SENSIBLE TASK
                        GotCommand(message, boat, this);
                    }
                }
                break;
                case 77: //M
                {
                    if(message.data != null)
                    {
                        HandleMissionCommand(message, boat, this);
                    }
                }
                break;
                case 86 : //V
                {
                    if(message != null)
                    {
                        VideoCommunication(message, boat);
                    }
                }
                break;
                case 83: //S
                {
                    if(message != null)
                        {
                            BoatClientAppStateManager(message, boat);
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

    private byte[] ReturnHeaderMissionArray(MissionParam missionParam)
    {
        byte[] payload_header_mission = new byte[83]; //(32 * 2) + 19
        try
        {
            byte[] arr_char_idMission = new byte[32];
            byte[] id_miss = Encoding.UTF8.GetBytes(missionParam.idMission);
            for (int i = 0; i < id_miss.Length; i++)
            {
                arr_char_idMission[i] = id_miss[i];
            }



            byte[] arr_char_idMissionNext = new byte[32];
            byte[] idMissNext = Encoding.UTF8.GetBytes(missionParam.idMissionNext);
            for (int i = 0; i < idMissNext.Length; i++)
            {
                arr_char_idMissionNext[i] = idMissNext[i];
            }

            

            var index = 0;

            index = CopyToByteArray(arr_char_idMission, payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.nMission), payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.total_mission_nWP), payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.wpStart), payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.cycles), payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.wpEnd), payload_header_mission, index);

            byte[] nmModeB = [missionParam.NMmode];
            index = CopyToByteArray(nmModeB, payload_header_mission, index);

            index = CopyToByteArray(BitConverter.GetBytes(missionParam.NMnum), payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.NMStartInd), payload_header_mission, index);
            index = CopyToByteArray(arr_char_idMissionNext, payload_header_mission, index);
            index = CopyToByteArray(BitConverter.GetBytes(missionParam.standRadius), payload_header_mission, index);

            return payload_header_mission;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"error while transforming mission header into byte array {ex.Message}");
            throw new StackOverflowException();
        }
        

        
    }

    
    private void GotCommand(EcodroneBoatMessage message, EcodroneBoat boat, EcoClient ecoclient)
    {
        _ = new cmdRW();

        if(message.data != null)
        {
            UploadMissionData? uploadMissionData = JsonConvert.DeserializeObject<UploadMissionData>(message.data.ToString());

            if(uploadMissionData != null)
            {
                List<WayPoint> waypoints = new List<WayPoint>();
                MissionParam missionParam = new MissionParam()
                {
                    idMission = uploadMissionData.missionParam.IdMission + '\0',
                    nMission = uploadMissionData.missionParam.MissionNumber,
                    total_mission_nWP = uploadMissionData.missionParam.TotalWayPoint,
                    wpStart = uploadMissionData.missionParam.WpStart,
                    cycles = uploadMissionData.missionParam.Cycles,
                    wpEnd = uploadMissionData.missionParam.WpEnd,
                    NMmode = (byte)uploadMissionData.missionParam.NMmode,
                    NMnum = uploadMissionData.missionParam.NMnum,
                    NMStartInd = uploadMissionData.missionParam.NMstart,
                    idMissionNext = "ARP12/21" + '\0',
                    standRadius = uploadMissionData.missionParam.StandRadius
            
                };

                if(uploadMissionData.pointslist.Count() > 0)
                {
                    int index_i = missionParam.wpStart;
                    int original_count = uploadMissionData.pointslist.Count;

                    for(int i=0; i < original_count; i++)
                    {
                        
                        if (index_i > original_count - 1)
                        {
                            index_i = 0;
                        }

                        UploadWaypoint uploadWaypoint = uploadMissionData.pointslist[index_i];

                        WayPoint wayPoint = new WayPoint
                        {
                            IndexWP = (ushort)index_i,
                            Nmissione = missionParam.nMission,
                            Latitude = uploadWaypoint.lat,
                            Longitude = uploadWaypoint.lng,
                            NavMode = uploadWaypoint.navmode,
                            PointType = uploadWaypoint.pointype,
                            MonitoringOp = uploadWaypoint.mon,
                            ArriveMode = uploadWaypoint.amode,
                            WaypointRadius = uploadWaypoint.wrad
                        };

                        waypoints.Add(wayPoint);
                        index_i++;
                    }
                }

                boat.teensySocketInstance._teensyLibParser.UpdateListWayPoints(waypoints);

                

                byte[] payload_header_mission = ReturnHeaderMissionArray(missionParam);

                byte[] command_array = [
                    cmdRW.ID_WEBAPP,
                    cmdRW.ID_MODULO_BASE,
                    cmdRW.ID_MODULO_BASE,
                    cmdRW.REQUEST_CMD1,
                    cmdRW.SAVE_MISSION_CMD2,
                    cmdRW.SAVE_MISSION_PARAM_CMD3,
                ];

                byte[] teensy_message_ready  = command_array.Concat(payload_header_mission).ToArray();

                
                TeensyMessageContainer tmessage = new TeensyMessageContainer("UpMission", teensy_message_ready
                , true)
                {
                    IdClient = IdClient
                };

                boat.teensySocketInstance.command_task_que.Add(tmessage);
            }
        
        	
           
        }
       
    } 

    private async void HandleMissionCommand(EcodroneBoatMessage stateMessage, EcodroneBoat boat, EcoClient? ecoClient)
    {
        if(stateMessage.scope != 'M' || ecoClient == null ||ecoClientRole != EcoClientRole.Admin)
        {
            return;
        }else
        {
            int type_m = ushort.Parse(stateMessage.type);
            switch (type_m)
            {
                case 0:
                {
                    _ = new cmdRW();

                    TeensyMessageContainer tmessage = new TeensyMessageContainer("DTree",
                    [
                        cmdRW.ID_WEBAPP,
                        cmdRW.ID_MODULO_BASE,
                        cmdRW.ID_MODULO_BASE,
                        cmdRW.REQUEST_CMD1,
                        cmdRW.UPDATE_MISS_LIST_CMD2,
                        cmdRW.UPDATE_FILE_LIST_CMD3,
                        0x00,
                        0x00
                    ], true)
                    {
                        IdClient = IdClient
                    };

                    boat.teensySocketInstance.command_task_que.Add(tmessage);
                }
                break;
                case 1:
                {
                    if(stateMessage.data != null)
                    {
                        string unencodedstring = Uri.UnescapeDataString((string)stateMessage.data);
                        byte[] path_choosen_file =  Encoding.UTF8.GetBytes(unencodedstring);
                        byte[] partial_command = [
                            cmdRW.ID_WEBAPP,
                            cmdRW.ID_MODULO_BASE,
                            cmdRW.ID_MODULO_BASE,
                            cmdRW.REQUEST_CMD1,
                            cmdRW.GET_MISSION_CMD2,
                            cmdRW.GET_MISSION_PARAM_CMD3,
                        ];

                        byte[] total_command = new byte[path_choosen_file.Length + partial_command.Length + 1];

                        Array.Copy(partial_command, total_command, partial_command.Length);
                        Array.Copy(path_choosen_file, 0, total_command, partial_command.Length, path_choosen_file.Length);
                        total_command[partial_command.Length + path_choosen_file.Length] = 0x00;

                        _ = new cmdRW();

                        TeensyMessageContainer tmessage = new TeensyMessageContainer("MMW", total_command
                        , true)
                        {
                            IdClient = IdClient
                        };

                        boat.teensySocketInstance.command_task_que.Add(tmessage);

                        // for (int i = 0; i < boat._boatclients.Count; i++)
                        // {
                        //     if(boat._boatclients[i].IdClient != IdClient)
                        //     {
                        //         boat._boatclients[i].src_cts_block_listening.Cancel();

                        //     }
                        // }
                    }
                }
                break;
                default:
                {

                }
                break;
            }
        }
        
    }

    

}
