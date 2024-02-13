using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Channels;
using webapi.Services.BusService;
using webapi.Utilities;
using static webapi.ITeensyMessageConstructParser;

namespace webapi.Services.NewTeensyService
{

    public class SocketTeensyService : ISocketTeensyService
    {
        private List<TeensySocketInstance> teensySocketInstances = new List<TeensySocketInstance>();
        private readonly ILogger<SocketTeensyService> _logger;
        private readonly IServiceProvider _serviceProvider;
        //private readonly IBusEvent _busEvent;
        private IVideoSocketSingleton _videoSocketSingleton;


        private event EventHandler<List<WayPoint>> HandlerListWayPoint;


        public SocketTeensyService(
            IVideoSocketSingleton videoSocketSingleton,
            ILogger<SocketTeensyService> logger,
            //IBusEvent busEvent,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _videoSocketSingleton = videoSocketSingleton;
            //_busEvent = busEvent;
        }


 

        public void GotCommand(object sender, BusEventMessage busEventMessage)
        {
            if (teensySocketInstances.Any(x => x.ecodroneBoat.MaskedId == busEventMessage.idTeensy))
            {
                TeensySocketInstance temp_t = teensySocketInstances
                .Single(x => x.ecodroneBoat.MaskedId == busEventMessage.idTeensy);


                if (busEventMessage.data[5] == cmdRW.SAVE_MISSION_PARAM_CMD3 && busEventMessage.data[4] == cmdRW.SAVE_MISSION_CMD2 && busEventMessage.data[3] == cmdRW.REQUEST_CMD1)
                {
                    byte[] command_array = busEventMessage.data.Take(6).ToArray();

                    var receivedMessage = Encoding.UTF8.GetString(busEventMessage.data.Skip(6).ToArray());

                    MissionDataPayload? payload = JsonConvert.DeserializeObject<MissionDataPayload>(receivedMessage);


                    if (payload != null)
                    {

                        List<WayPoint> waypoints = new List<WayPoint>();
                        MissionParam param = new MissionParam();


                        param.idMission = payload.MissionParam.IdMission + '\0';
                        param.nMission = payload.MissionParam.MissionNumber;
                        param.total_mission_nWP = payload.MissionParam.TotalWayPoint;
                        param.wpStart = payload.MissionParam.WpStart;
                        param.cycles = payload.MissionParam.Cycles;
                        param.wpEnd = payload.MissionParam.WpEnd;
                        param.NMmode = (byte)payload.MissionParam.NMmode;
                        param.NMnum = payload.MissionParam.NMnum;
                        param.NMStartInd = payload.MissionParam.NMstart;
                        param.idMissionNext = "ARP12/21" + '\0';
                        param.standRadius = payload.MissionParam.StandRadius;


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
                        _logger.LogInformation(arridMission.Length.ToString());


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

                        Debug.WriteLine("done");

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

                        HandlerListWayPoint += temp_t._teensyLibParser.UpdateListWayPoints;
                        HandlerListWayPoint(this, waypoints);
                        HandlerListWayPoint -= temp_t._teensyLibParser.UpdateListWayPoints;


                        temp_t.command_task_que.Add(
                            new MessageContainerClass("new_user_command", combinedArray, needPreparation: true)
                        );

                    }
                }
                else
                {
                    temp_t.command_task_que.Add(
                    new MessageContainerClass("new_user_command", busEventMessage.data, needPreparation: true));
                    
                }

            }
        }

        private int CopyToByteArray(byte[] source, byte[] destination, int startIndex)
        {
            Array.Copy(source, 0, destination, startIndex, source.Length);
            return startIndex + source.Length;
        }


        public void NewClientConnectionEvent(object sender, NewClientEventArgs newclientevent)
        {


            if (!teensySocketInstances.Any(x => x.ecodroneBoat.MaskedId == newclientevent.maskedTeensyId))
            {
                EcodroneBoat ecodroneBoat = new EcodroneBoat([0x10, 0x11, 0x12], "2.194.18.251", 5050, true); //"2.194.20.182"
                TeensySocketInstance socketInstance = new TeensySocketInstance(ecodroneBoat, newclientevent.maskedTeensyId);
                socketInstance.TaskReading = StartTeensyTalk(socketInstance);

                Tuple<int, VideoTcpListener?> newListener = _videoSocketSingleton.CreateVideoTcpListener(newclientevent.maskedTeensyId);

                if (newListener.Item1 == -1 || newListener.Item2 == null)
                {
                    throw new NotImplementedException();
                }



                teensySocketInstances.Add(socketInstance);
            }

            /*if (teensySocketInstances.Find(x => x.ecodroneBoat.MaskedId == newclientevent.maskedTeensyId) != null)
            {
                TeensySocketInstance teensyInst = teensySocketInstances.Single(x => x.ecodroneBoat.MaskedId == newclientevent.maskedTeensyId);

                if (!teensyInst.jetsonSocket.client_video.Any(x => x.IdClient == newclientevent.userid))
                {
                    EcoClient ecoClient = new EcoClient();
                    ecoClient.IdClient = newclientevent.userid;
                    ecoClient.groupRole = GroupRole.Admin;

                    teensyInst.jetsonSocket.client_video.Add(ecoClient);
                }

                _logger.LogInformation("Client connected " + newclientevent.maskedTeensyId);
            }*/
        }

        public ChannelTeensyMessage? ReadOnChannel(string mIdTeensy)
        {
            if (!teensySocketInstances.Any(x => x.ecodroneBoat.MaskedId == mIdTeensy))
            {
                return null;

            }else
            {
                Channel<ChannelTeensyMessage> channel = teensySocketInstances.Single(x => x.ecodroneBoat.MaskedId == mIdTeensy).channelTeensy;
                if(channel.Reader.Count != 0)
                {
                    ChannelTeensyMessage? channelRead;
                    channel.Reader.TryRead(out channelRead);
                    
                    return channelRead;
                }

                return null;
            }   
        }

        public void WriteOnChannel(string mIdTeensy, ChannelTeensyMessage? channelMessage)
        {
            if (teensySocketInstances.Any(x => x.ecodroneBoat.MaskedId == mIdTeensy))
            {
                Channel<ChannelTeensyMessage> channel = teensySocketInstances.Single(x => x.ecodroneBoat.MaskedId == mIdTeensy).channelTeensy;
                channel.Writer.WriteAsync(channelMessage);
            }
        }

        private List<MessageContainerClass> GenerateRequestFunct()
        {
            List<MessageContainerClass> _containers_message = new List<MessageContainerClass>();

            _containers_message.Add(
                new MessageContainerClass("ImuData",
                [
                    cmdRW.ID_WEBAPP,
                    cmdRW.ID_MODULO_BASE,
                    cmdRW.ID_IMU,
                    cmdRW.REQUEST_CMD1,
                    cmdRW.IMU_GET_CMD2,
                    cmdRW.IMU_RPY_ACC_CMD3
                ])
            );

            return _containers_message;
        }

        


        public async Task StartTeensyTalk(TeensySocketInstance socketInstance)
        {
            using (socketInstance.TcpSocket = new TcpClient(socketInstance.ecodroneBoat._IPTeensy, socketInstance.ecodroneBoat._PortTeensy))
            {
                using (socketInstance.NetworkStream = socketInstance.TcpSocket.GetStream())
                {
                    socketInstance.task_que = this.GenerateRequestFunct();

                    while (socketInstance.TcpSocket.Connected)
                    {

                        ///Debug.WriteLine("TH Group Id is: " + socketInstance.ecodroneBoat.MaskedId + " socket state is : " + (socketInstance.TcpSocket.Connected ? "connected" : "not connected"));

                        while(socketInstance.command_task_que.Count > 0)
                        {
                            ChannelTeensyMessage commandDataC = await TalkToTeensyAsync(socketInstance.command_task_que[0], socketInstance);
                            socketInstance.command_task_que.RemoveAt(0);
                            GetCommandWriteChannel(commandDataC, socketInstance);

                        }

                        ChannelTeensyMessage channelData = await TalkToTeensyAsync(socketInstance.task_que[0], socketInstance);

                        socketInstance.task_que.RemoveAt(0);
                        GetCommandWriteChannel(channelData, socketInstance);

                        if (socketInstance.task_que.Count == 0)
                        {
                            socketInstance.task_que = GenerateRequestFunct();
                        }

                        await Task.Delay(100);
                    }

                }
            }
        }

        private void GetCommandWriteChannel(ChannelTeensyMessage channelData, TeensySocketInstance teensySocketInstance)
        {
            if (channelData.data_in != null)
            {
                WriteOnChannel(teensySocketInstance.ecodroneBoat.MaskedId, channelData);
            }

            //if there is a new issued command then add it to the list of tasks at position 0
            if (channelData.data_command != null && channelData.NeedAnswer)
            {
                teensySocketInstance.command_task_que.Add(
                    new MessageContainerClass("new_internal_data", channelData.data_command, needPreparation: false)
                );

            }

        }


        private async Task<ChannelTeensyMessage> TalkToTeensyAsync(MessageContainerClass m_cont, TeensySocketInstance socketInstance/*, TeensyGroup TG*/)
        {
            ChannelTeensyMessage channelMessage = new ChannelTeensyMessage();

           

            byte[] data = m_cont.CommandId;

            if (m_cont.NeedPreparation)
            {
                data = socketInstance._teensyLibParser.PrepareTeensyRequest(m_cont.CommandId);
            }

            if (socketInstance.NetworkStream != null)
            {
                await socketInstance.NetworkStream.WriteAsync(data, 0, data.Length, CancellationToken.None);

                channelMessage = await socketInstance._teensyLibParser.ReadBufferAsync(socketInstance.NetworkStream);


                return channelMessage;
               
                    
            }
            else
            {
                _logger.LogWarning("Error with network stream");
            }

            return channelMessage;
        }
    }
}

