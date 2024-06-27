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
    public WebSocket _socketClient {get; set;}
    public ClientCommunicationStates appState = ClientCommunicationStates.SENSORS_DATA;

    public CancellationTokenSource src_cts_client = new CancellationTokenSource();
    public CancellationToken cts_client {get; private set;}
    public Task? taskina {get; set;}
    public EcoClientRole ecoClientRole = EcoClientRole.Admin;
    public EcodroneBoat ecodroneBoat {get; private set;}
    

    public bool isListening = true;
    public EcoClient(EcodroneBoat _ecodroneBoat, WebSocket socket)
    {
        cts_client = src_cts_client.Token;
        ecodroneBoat = _ecodroneBoat;
        _socketClient = socket;
        taskina = Task.Factory.StartNew(GetOwnershipOfWebSocket, cts_client);
        //src_cts_block_listening = CancellationTokenSource.CreateLinkedTokenSource(cts_block_listening);
        
    }

    public async Task<bool> GetOwnershipOfWebSocket() 
    {
    
        await SendFirstMessage();
        ecodroneBoat.signalBusSocket.Subscribe(TeensyReadAndSend, this);

        while (_socketClient.State == WebSocketState.Open)
        {
            var buffer = new byte[1024 * 4];

            WebSocketReceiveResult receiveResult = await _socketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            Debug.WriteLine($"message type {receiveResult.MessageType}" );
            ReadClientSocket(buffer, receiveResult.Count);

        }


        _socketClient.Dispose();

        ecodroneBoat.signalBusSocket.Unsubscribe(TeensyReadAndSend, this);
        ecodroneBoat._boatclients.Remove(this); 
                

        // 
        Debug.WriteLine("DEBUG CLIENT SOCKET IS HERE");

        return true;
        // //await _socketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
        
        // _socketClient.Dispose();

        // //please check if remove happen these are different task maybe implement signal or message on bus
        
        // ecodroneBoat.signalBusSocket.Unsubscribe(TeensyReadAndSend, this);
        // ecodroneBoat._boatclients.Remove(this); 

        // await src_cts_client.CancelAsync();

    }

    
    private async Task SendFirstMessage()
    {
        IdClient = Guid.NewGuid().ToString();

        EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
        {
            scope = 'U',
            type = "1",
            uuid = ecodroneBoat.maskedId,
            direction = IdClient,
            identity = IdClient,
            data = null
        };

        string message_serialized = JsonConvert.SerializeObject(ecodroneBoatMessage);
        Debug.WriteLine(message_serialized);
        var messageToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message_serialized));

        await _socketClient.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);

        
    }

    public void TeensyReadAndSend(SignalBusMessage messageTeensy)
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
                        
                        if(appState == ecodroneBoat.ecodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id))
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
                        
                        if(appState == ecodroneBoat.ecodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id)) 
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
                        if(appState == ecodroneBoat.ecodroneMessagesContainers.CheckAllowedContainer(messageTeensy.message_id)) // && messageTeensy.id_client != null && messageTeensy.id_client == IdClient
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
                        ecodroneBoatMessage.scope = 'N';
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

    public async void SerializeAndSendMessage(EcodroneBoatMessage ecodroneMessage)
    {
        if(_socketClient != null)
        {
            if (ecodroneMessage.direction == IdClient)
            {
                string message_serialized = JsonConvert.SerializeObject(ecodroneMessage, Formatting.Indented);

                Debug.WriteLine(message_serialized);

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
                            appState = ClientCommunicationStates.SENSORS_DATA;
                        }
                        break;
                        case "MSS":
                        {
                            appState = ClientCommunicationStates.MISSIONS;
                        }
                        break;
                        case "VID":
                        {
                            if(!boat._videoBusService.IsASubscriber(IdClient))
                            {
                                boat._videoBusService.Subscribe(SerializeAndSendMessage, IdClient);
                            }
                            
                            appState = ClientCommunicationStates.VIDEO;
                        }
                        break;
                        case "WPY":
                        {
                            appState = ClientCommunicationStates.WAYPOINT;
                        }
                        break;
                        case "NAV":
                        {
                            appState = ClientCommunicationStates.NAVIGATION;
                            _ = new cmdRW();

                            byte[] command = [ 
                                cmdRW.ID_WEBAPP,
                                cmdRW.ID_MODULO_BASE,
                                cmdRW.ID_MODULO_BASE,
                                cmdRW.REQUEST_CMD1,
                                cmdRW.REMOTE_CONTROL_CMD2,
                                cmdRW.INPUT_JOYSTICK_CMD3
                            ];

                            
                            byte[] buffer_ready_container = ecodroneBoat._teensyLibParser.SendConstructBuff(command, null);

                            TeensyMessageContainer tmessage = new TeensyMessageContainer("NavStart", buffer_ready_container, IdClient);

                            //REACTIVATE
                            boat.teensySocketInstance.command_task_que.Add(tmessage);
                        }
                        break;
                    }
                }
                break;
            }
            
        }
    }

    public void UnsubscribeVideo(EcodroneBoat _boat)
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
                            // if(videoMessage.data != null)
                            // {
                               
                            // }
                            videoMessage.identity = IdClient;
                            videoMessage.data = videoMessage?.data?.ToString();
                            boat._videoBusService.Publish(videoMessage);
                        }
                        break;
                    }
                    
                }
                break;
                default:
                {
                    // if(videoMessage.data != null)
                    // {
                        
                    // }
                    videoMessage.data = videoMessage?.data?.ToString();
                    boat._videoBusService.Publish(videoMessage);
                    
                }
                break;
            }
        }

    }

    public void ReadClientSocket(byte[] bugger, int receiveResultCount)
    {
        
        byte message_scope = bugger.First();
    
        if(ecodroneBoat.IsCorrectByte(message_scope))
        {
            
            byte[] message_length = new byte[4];
            byte[] byte_message = new byte[receiveResultCount - 4];
            
            Array.Copy(bugger, 1, message_length, 0, message_length.Length);
            Array.Copy(bugger, 5, byte_message, 0, byte_message.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(message_length); // Reverse the byte array if the system is little-endian
            }
            
            EcodroneBoatMessage? message = JsonConvert.DeserializeObject<EcodroneBoatMessage>(Encoding.UTF8.GetString(byte_message));
            
            if(message != null)
            {
                switch (message_scope)
                {
                    case 72: //W
                    {
                        if(message.data != null)
                        {
                            UploadMission(message, ecodroneBoat);
                        }
                    }
                    break;
                    case 77: //M
                    {
                        if(message.data != null)
                        {
                            HandleMissionCommand(message, ecodroneBoat);
                        }
                    }
                    break;
                    case 86 : //V
                    {
                        if(message != null)
                        {
                            VideoCommunication(message, ecodroneBoat);
                        }
                    }
                    break;
                    case 83: //S
                    {
                        if(message != null)
                        {
                            BoatClientAppStateManager(message, ecodroneBoat);
                        } 
                    }
                    break;
                    case 78: //N
                    {
                        if(message != null)
                        {
                        
                            if(message.data != null && message.data.ToString() != null)
                            {
                                _ = new cmdRW();

                                string? data = message.data.ToString();
                                Debug.WriteLine("Message client " + data);

                                if(data != null)
                                {
                                    byte[] joy_command = [ 
                                        cmdRW.ID_WEBAPP,
                                        cmdRW.ID_MODULO_BASE,
                                        cmdRW.ID_MODULO_BASE,
                                        cmdRW.REQUEST_CMD1,
                                        cmdRW.JS_DRIVING_DATA_CMD2,
                                        0 //ask if it is 4 here
                                    ];

                                    NavigationData? navigationData =  JsonConvert.DeserializeObject<NavigationData>(data);
                                    
                                    if(navigationData.buttons.Any(x => x != 0))
                                    {
                                        Debug.WriteLine("button received");
                                    }
                                    

                                    //Debug.WriteLine(string.Join(",", navigationData.buttons));
                                    Debug.WriteLine(string.Join("Axis Y", navigationData.axisY));
                                    Debug.WriteLine(string.Join("Axis X", navigationData.axisX));
                                    Debug.WriteLine(string.Join("Throttle  ", navigationData.throttle));
                                    Debug.WriteLine(string.Join("POV ", navigationData.POV));
                                    Debug.WriteLine(string.Join("Wheel ", navigationData.wheel));

                                    if(navigationData != null)
                                    {
                                        byte[] nav_data = new byte[35];
                                        Array.Copy(navigationData.buttons, 0, nav_data, 0, 17);
                                        
                                        Array.Copy(BitConverter.GetBytes(navigationData.POV), 0, nav_data, 17, 2);
                                        Array.Copy(BitConverter.GetBytes(navigationData.axisX), 0, nav_data, 19, 4);
                                        Array.Copy(BitConverter.GetBytes(navigationData.axisY), 0, nav_data, 23, 4);
                                        Array.Copy(BitConverter.GetBytes(navigationData.wheel), 0, nav_data, 27, 4);
                                        Array.Copy(BitConverter.GetBytes(navigationData.throttle), 0, nav_data, 31, 4);


                                        byte[] container_ship_nav_data = ecodroneBoat._teensyLibParser.SendConstructBuff(joy_command, nav_data);
                                        
                                        TeensyMessageContainer tmessage = new TeensyMessageContainer("NavData", container_ship_nav_data, IdClient);

                                        StringBuilder sn = new StringBuilder();

                                        foreach (byte b in container_ship_nav_data)
                                        {
                                            sn.Append(b + " ");
                                        }
                                        Debug.WriteLine("NAV DATA");
                                        Debug.WriteLine(sn.ToString().Trim());
                                        
                                        //REACTIVATE
                                        ecodroneBoat.teensySocketInstance.command_task_que.Add(tmessage);

                                    }
                                }

                                
                            }
                            
                        } 
                    }
                    break;
                    default:
                    break;
                }
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

    
    private void UploadMission(EcodroneBoatMessage message, EcodroneBoat boat)
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
                    //int index_i = missionParam.wpStart;
                    int original_count = uploadMissionData.pointslist.Count;

                    for(int i=0; i < original_count; i++)
                    {
                        
                        // if (index_i > original_count - 1)
                        // {
                        //     index_i = 0;
                        // }

                        UploadWaypoint uploadWaypoint = uploadMissionData.pointslist[i];
                        //SantoriniSampietrini99!
                        //2a00:6d42:1242:169a::1

                        WayPoint wayPoint = new WayPoint
                        {
                            IndexWP = (ushort)i,
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
                        // index_i++;
                    }
                }

                boat._teensyLibParser.UpdateListWayPoints(waypoints);

                

                byte[] payload_header_mission = ReturnHeaderMissionArray(missionParam);

                byte[] command_array = [
                    cmdRW.ID_WEBAPP,
                    cmdRW.ID_MODULO_BASE,
                    cmdRW.ID_MODULO_BASE,
                    cmdRW.REQUEST_CMD1,
                    cmdRW.SAVE_MISSION_CMD2,
                    cmdRW.SAVE_MISSION_PARAM_CMD3,
                ];


                byte[] buffer_ready_container = ecodroneBoat._teensyLibParser.SendConstructBuff(command_array, payload_header_mission);
                TeensyMessageContainer tmessage = new TeensyMessageContainer("UpMission", buffer_ready_container);

                boat.teensySocketInstance.command_task_que.Add(tmessage);
            }
        
        	
           
        }
       
    } 

    private void HandleMissionCommand(EcodroneBoatMessage stateMessage, EcodroneBoat boat)
    {
        if(stateMessage.scope != 'M' || ecoClientRole != EcoClientRole.Admin)
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

                    byte[] command = [ 
                        cmdRW.ID_WEBAPP,
                        cmdRW.ID_MODULO_BASE,
                        cmdRW.ID_MODULO_BASE,
                        cmdRW.REQUEST_CMD1,
                        cmdRW.UPDATE_MISS_LIST_CMD2,
                        cmdRW.UPDATE_FILE_LIST_CMD3
                    ];

                    
                    byte[] directory_tree_index = [0x00, 0x00];
                    byte[] buffer_ready_container = ecodroneBoat._teensyLibParser.SendConstructBuff(command, directory_tree_index);


                    TeensyMessageContainer tmessage = new TeensyMessageContainer("DTree", buffer_ready_container, IdClient);
                    boat.teensySocketInstance.command_task_que.Add(tmessage);
                }
                break;
                case 1:
                {
                    if(stateMessage.data != null)
                    {
                        string unencodedstring = Uri.UnescapeDataString((string)stateMessage.data);

                        byte[] byte_path =  Encoding.UTF8.GetBytes(unencodedstring);
                        byte[] byte_path_nll_terminated = new byte[byte_path.Length + 1];
                        Array.Copy(byte_path, 0, byte_path_nll_terminated, 0, byte_path.Length);
                        byte_path_nll_terminated[byte_path.Length] = 0x00;

                        
                        byte[] partial_command = [
                            cmdRW.ID_WEBAPP,
                            cmdRW.ID_MODULO_BASE,
                            cmdRW.ID_MODULO_BASE,
                            cmdRW.REQUEST_CMD1,
                            cmdRW.GET_MISSION_CMD2,
                            cmdRW.GET_MISSION_PARAM_CMD3,
                        ];


                        byte[] buffer_ready_container = ecodroneBoat._teensyLibParser.SendConstructBuff(partial_command, byte_path_nll_terminated);

                        TeensyMessageContainer tmessage = new TeensyMessageContainer("MMW", buffer_ready_container, IdClient);
                        boat.teensySocketInstance.command_task_que.Add(tmessage);

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
