using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace webapi.Controllers 
{
    [Route("service/[controller]")]
    [ApiController]
    public class BoatServiceController : ControllerBase
    {
        //all of this could be moved to Ecodrone Boat for multiple boat
        private int port = 5058;
        private HttpListener _boatListener;
        private Thread _threadBoatListener;
        public Thread _threadJetsonListener;
        //

        private EcodroneBoat ecodroneBoat {get; set;}


        public BoatServiceController()
        {
            _boatListener = new HttpListener();
            _boatListener.Prefixes.Add($"http://localhost:{port}/");
        }

        [HttpPost("ActivateBoat")]
        public IActionResult ActivateBoat([FromBody] string boatid)
        {
            
            //from boatid get database to fill this data below
            if(true)
            {
                byte[] _sync_teensy = [0x10, 0x11, 0x12];
                string _ipAddress = "192.168.1.213";
                int _port = 5050;
                bool _active = true;

                string _teensy_masked_id = boatid; //"replace_me_with_id_from_database";
                ecodroneBoat = new EcodroneBoat(_teensy_masked_id, _sync_teensy, _ipAddress, _port, _active); //"2.194.20.182" 2.194.18.251 2.194.23.215
                ecodroneBoat.teensySocketInstance.TaskReading = ecodroneBoat.teensySocketInstance.StartTeensyTalk();

                _threadBoatListener = new Thread(new ThreadStart(callBack_BoatListener));
                _threadJetsonListener = new Thread(new ThreadStart(ecodroneBoat.ecodroneVideo.ListenJetson));

                _boatListener.Start();
                _threadBoatListener.Start();
                _threadJetsonListener.Start();
                
                
                return Ok();
            }
        
        }

        [HttpGet("DeactivateBoat")]
        public IActionResult DeactivateBoat(string boatid)
        {
            //from boatid get database
            if(true)
            {
                //stop boat services
                //remember to close all connections and advise clients if are connected
                ecodroneBoat.teensySocketInstance.TaskReading?.Dispose();
                ecodroneBoat.teensySocketInstance._networkStream?.Close();
                ecodroneBoat.teensySocketInstance._teensySocket?.Close();
                ecodroneBoat.teensySocketInstance._teensySocket?.Dispose();

                return Ok();
            }

        }


        private async void callBack_BoatListener()
        {
            while (true)
            {
                
                HttpListenerContext _client_context = await _boatListener.GetContextAsync();
                Debug.WriteLine($"boat {ecodroneBoat.maskedId} listener is listening for receiver of teensy data ");

                Task single_client_task = Task.Factory.StartNew(async () =>
                {
                    HttpListenerWebSocketContext websocket_context = await _client_context.AcceptWebSocketAsync(null);
                    Debug.WriteLine($"boat {ecodroneBoat.maskedId} listener got client {websocket_context.User} connected.");

                    if (websocket_context.IsSecureConnection)
                    {
                        Debug.WriteLine("is secure connection");
                    }
                    if(websocket_context.IsAuthenticated)
                    {
                        Debug.WriteLine("connection is authenticated");
                    }

                    Task single_client_task = Task.Factory.StartNew(async () =>
                    {
                        await HandlingClient(websocket_context.WebSocket);

                        //ecodroneBoat.teensyInst.jetsonSocket.client_video.Add(ecoClient);

                        //_logger.LogInformation("Client connected " + newclientevent.maskedTeensyId);
                    });
                });
            }
                    
        }


        

        #region toImplement
        private bool BufferIsComplete(EcoClient ecoClient)
        {
            // if(message_length_uint != (receiveResult.Count - 9))
            // {
            //     //message is partial
            //     if(ecoClient != null)
            //     {
            //         if(ecoClient.message_length == 0)
            //         {
            //             ecoClient.message_length = message_length_uint;
            //             ecoClient.buffer_bytes =  new byte[message_length_uint];
                        
            //             Array.Copy(byte_message, ecoClient.buffer_bytes, 0);
            //             ecoClient.bytecopied = (uint)byte_message.Length;

            //         }else
            //         {
            //             if(ecoClient.buffer_bytes != null)
            //             {
            //                 if(ecoClient.message_length <= (ecoClient.bytecopied + receiveResult.Count - 9))
            //                 uint bytetocopy = (uint)ecoClient.buffer_bytes.Length - ecoClient.bytecopied;
            //                 Array.Copy(byte_message, 0, ecoClient.buffer_bytes, ecoClient.bytecopied, bytetocopy);
            //             }
                        
            //         }
                    
            //     }
            // }

            return true;
        }
        #endregion

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

                ecodroneBoat._boatclients.Add(_client);

                //publish id for user
                EcodroneBoatMessage ecodroneBoatMessage = new EcodroneBoatMessage()
                {
                    scope = 'U',
                    type = "1",
                    uuid = ecodroneBoat.maskedId,
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
        private async Task HandlingClient(WebSocket webSocket) 
        {

            if(webSocket.State == WebSocketState.Open)
            {
                EcoClient? ecoClient = null;
               
                while (webSocket.State == WebSocketState.Open)
                {
                    if(ecoClient != null)
                    {
                        //List<Task> boat_racing_tasks = [ReadWebSocket(webSocket)];
                        Task boat_racing_tasks = ecodroneBoat.ReadWebSocket(webSocket, ecoClient);

                        if(!ecodroneBoat._videoBusService.IsASubscriber(ecoClient.IdClient))
                        {
                            ecodroneBoat._videoBusService.Subscribe(ecoClient.SerializeAndSendMessage, ecoClient.IdClient);
                        }

                        while(!boat_racing_tasks.IsCompleted)
                        {
                            await TeensyChannelReadAndSendData(ecoClient);
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

                if(ecoClient != null)
                {
                    ecodroneBoat._boatclients.Remove(ecoClient);
                    if(ecoClient._socketClient != null)
                    {
                        await ecoClient._socketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, $"{ecoClient.IdClient} bdisconnected", CancellationToken.None);
                        ecoClient._socketClient.Dispose();
                    }
                }
                
                
            }
        }

        private async Task TeensyChannelReadAndSendData(EcoClient ecoClient)
        {
            ChannelTeensyMessage? messageTeensy = ecodroneBoat.teensySocketInstance.ReadOnChannel();

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
                                uuid = ecodroneBoat.maskedId,
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
}