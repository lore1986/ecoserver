using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using webapi;

public class EcoClient
{
    public string IdClient { get; set; } = "NNN";
    public WebSocket? _socketClient {get; set;}
    public ClientCommunicationStates appState = ClientCommunicationStates.SENSORS_DATA;

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
                    //unload everything
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
                                    //advise client disconnected
                                    //remove client from server 
                                    EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                                    {
                                        scope = 'U',
                                        type = "0",
                                        uuid = IdClient,
                                        identity = IdClient,
                                        data = "data",
                                        direction = "jetson_id",
                                    };
                                    
                                    
                                    boat._videoBusService.Publish(ecodroneBoatMessage);
                                    boat._videoBusService.Unsubscribe(this.SerializeAndSendMessage, IdClient);

                                    //justTotest
                                    // boat._boatclients.Remove(this);

                                    
                                    // await boat.teensySocketInstance.cts.CancelAsync();
                                    // if(boat.teensySocketInstance.TaskReading != null)
                                    // {
                                    //     boat.teensySocketInstance.TaskReading.Wait(boat.teensySocketInstance._cancellationToken);
                                    // }

                                    // boat.teensySocketInstance.cts.Dispose();
                                    
                                }

                                appState = ClientCommunicationStates.SENSORS_DATA;
                            }
                        }
                        break;
                        case "MSS":
                        {
                            if(appState != ClientCommunicationStates.MISSIONS)
                            {
                                appState = ClientCommunicationStates.MISSIONS;
                            }
                        }
                        break;
                        case "VID":
                        {
                            if(appState != ClientCommunicationStates.VIDEO)
                            {
                                appState = ClientCommunicationStates.VIDEO;
                            }
                        }
                        break;
                        // case "MAP":
                        // {
                        //     if(appState != ClientCommunicationStates.MISSIONS)
                        //     {
                                
                        //     }
                        // }
                        // break;
                        // case "MEM":
                        // {
                        //     if(appState != ClientCommunicationStates.)
                        //     {
                                
                        //     }
                        // }
                        // break;
                    }
                }
                break;
            }
            
        }
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

    

}
