using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;


//using webapi.Controllers;
using webapi.Utilities;
using static webapi.ITeensyMessageConstructParser;


//8 OClock commit

namespace webapi
{

    public class TeensyMessageConstructParser : ITeensyMessageConstructParser
    {

        private cmdRW _cmdRW  { get; set; }
        public string _url { get; private set; } = string.Empty;
        public int _port { get; private set; } = 0;
        public readonly EcodroneBoat ecodroneBoat;
        private List<WayPoint>? wayPoints = null;
        private UInt16 nWP_now { get; set; } = 0;
        private UInt16 totalWaypoints { get; set; } = 0;

        public TeensyMessageConstructParser(EcodroneBoat boat)
        {
            //_paramId = new ParamId();
            _cmdRW = new cmdRW();

            ecodroneBoat = boat;
        }



        public void UpdateListWayPoints(List<WayPoint> wayPoints)
        {
            this.wayPoints = wayPoints;
        }

        public void ConnectSocketTeensy(string uri, int port)
        {
            _url = uri;
            _port = port;
        }

        public byte[] SeriaDataAndReturn(string idData, string data)
        {
            TMS newTms = new TMS
            {
                MessageType = idData,
                MessageData = data
            };

            string json_data = JsonConvert.SerializeObject(newTms);
            byte[] _dataSend = Encoding.UTF8.GetBytes(json_data);
            return _dataSend;
        }


        public byte[] SendConstructBuff(byte[] command,  byte[]? object_array)
        {
            byte[] commandReady;

            if(object_array != null)
            {
                commandReady = new byte[object_array.Length + 11];
                Array.Copy(object_array, 0, commandReady, 10, object_array.Length);

            }else
            {
                commandReady = new byte[11];
            }
           
            commandReady[cmdRW.INDEX_SINCHAR_0] = ecodroneBoat._Sync[0];
            commandReady[cmdRW.INDEX_SINCHAR_1] = ecodroneBoat._Sync[1];
            commandReady[cmdRW.INDEX_SINCHAR_2] = ecodroneBoat._Sync[2];

            commandReady[cmdRW.INDEX_BUF_LENG] = (byte)(commandReady.Length - 3);
            commandReady[cmdRW.INDEX_BUF_SORG] = command[0];
            commandReady[cmdRW.INDEX_BUF_DEST] = command[1];
            commandReady[cmdRW.INDEX_BUF_ID_D] = command[2];

            commandReady[cmdRW.INDEX_BUF_CMD_1] = command[3];
            commandReady[cmdRW.INDEX_BUF_CMD_2] = command[4];
            commandReady[cmdRW.INDEX_BUF_CMD_3] = command[5];
    
            byte ck = CksumCompute(commandReady);
            commandReady[commandReady.Length - 1] = ck;

            byte[] checksumarraytest = new byte[commandReady.Length - 3];
            Array.Copy(commandReady, 3, checksumarraytest, 0, checksumarraytest.Length);

            bool res = cksumTest(checksumarraytest);

            if(res != true)
            {throw new OverflowException();}


            return commandReady;
        }

      

        public void ParseTeensyMessage(byte[] dataread)
        {
        
            byte[] cksummessage = new byte[dataread[3]];
            Array.Copy(dataread, 3, cksummessage, 0, cksummessage.Length);
          
            if (cksumTest(cksummessage))
            {
                byte[] array_command = new byte[7];
                Array.Copy(cksummessage, 0, array_command, 0, array_command.Length);

                byte[] message_array = new byte[cksummessage.Length - 8];
                Array.Copy(cksummessage, 7, message_array, 0, message_array.Length );

                analBuff(message_array, array_command);
            }
            else
            {
                Debug.WriteLine("error");
            }
            
        }

        private byte CksumCompute(byte[] buff)
        {
            byte cksum = 0;
            int indexBufLeng = 3; 

            for (int i = indexBufLeng; i < buff.Length - 1 ; i++)
            {
                cksum += buff[i];
            }

            return cksum;
        }

        
        private T CombinaVar<T>(byte[] bytes_convert)
        {
            T _template = default(T);

            GCHandle gcHandle = GCHandle.Alloc(bytes_convert, GCHandleType.Pinned);
            _template = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();

            return (T)_template;
        }

        private static Tuple<T?, int> CombinaMultiVar<T>(int index_now, byte[] buff_extract, T object_template)
        {
            T _template = default(T);

            int buff_size = Marshal.SizeOf(object_template);
            byte[] buff_object = new byte[buff_size];

            for (int i = 0; i < buff_size; i++)
            {
                buff_object[i] = buff_extract[index_now];
                index_now++;
            }

            GCHandle gcHandle = GCHandle.Alloc(buff_object, GCHandleType.Pinned);
            _template = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();

            return Tuple.Create(_template, index_now);
        }



        private bool cksumTest(byte[] buff)
        {
             byte cksum = 0;

            for (byte i = 0; i < (buff[0] - 1); i++)
            {
                cksum += buff[i];

            }

            if (cksum == buff[buff[0] - 1])
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string PrintCleanAnal(byte[] buff, out string mes_string)
        {
            string my_buff_text = "";

            for (int i = 0; i < buff.Length; i++)
            {
                my_buff_text += buff[i].ToString();
                my_buff_text += ',';
            }

            mes_string = my_buff_text;


            return mes_string;

        }

        private byte[] BufferDataOnly(byte[] buff)
        {
            byte[] buff_out = new byte[buff.Length - 8];
            Array.Copy(buff, 7, buff_out, 0, buff_out.Length);

            return buff_out;
        }


        private void analBuff(byte[] bufferDataOnly, byte[] mycommanddata)
        {
            TeensyMessage _teensyMessage = new TeensyMessage(mycommanddata);

            //byte[] bufferDataOnly = BufferDataOnly(mybuffdata);

            if ((_teensyMessage.destCmd) == cmdRW.ID_WEBAPP)
            {

                switch (_teensyMessage.id_dCmd)
                {
                    case cmdRW.ID_POWER:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.POWER_EN_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.POWER_EN_GET_CMD3)
                                {

                                }
                            }
                        }
                        break;

                    case cmdRW.ID_MODULO_BASE:

                        if (_teensyMessage.Cmd1 == cmdRW.REQUEST_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.SAVE_MISSION_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.SAVE_MISSION_WP_CMD3)
                                {


                                    nWP_now = CombinaVar<UInt16>(bufferDataOnly);
                                    Debug.WriteLine(nWP_now.ToString());

                                    if(wayPoints != null && wayPoints.Count > 0)
                                    {
                                        if(nWP_now < wayPoints.Count)
                                        {
                                            WayPoint wayPoint = wayPoints[nWP_now];

                                            byte[] wayPoint_bytes = new byte[20];
                                            int index = 0;

                                            byte[] nmisi = BitConverter.GetBytes(wayPoint.Nmissione);
                                            Array.Copy(nmisi, 0, wayPoint_bytes, index, nmisi.Length);
                                            index = index + nmisi.Length;


                                            byte[] iwp = BitConverter.GetBytes(wayPoint.IndexWP);
                                            Array.Copy(iwp, 0, wayPoint_bytes, index, iwp.Length);
                                            index = index + iwp.Length;

                                            byte[] lat_byte = BitConverter.GetBytes(wayPoint.Latitude);
                                            Array.Copy(lat_byte, 0, wayPoint_bytes, index, lat_byte.Length);
                                            index = index + lat_byte.Length;

                                            byte[] lng_byte = BitConverter.GetBytes(wayPoint.Longitude);
                                            Array.Copy(lng_byte, 0, wayPoint_bytes, index, lng_byte.Length);
                                            index = index + lng_byte.Length;

                                            wayPoint_bytes[index++] = wayPoint.NavMode;
                                            wayPoint_bytes[index++] = wayPoint.PointType;
                                            wayPoint_bytes[index++] = wayPoint.MonitoringOp;
                                            wayPoint_bytes[index++] = wayPoint.ArriveMode;

                                            byte[] rad_bytes = BitConverter.GetBytes(wayPoint.WaypointRadius);
                                            Array.Copy(rad_bytes, 0, wayPoint_bytes, index, rad_bytes.Length);


                                            Debug.WriteLine("done");


                                            if (wayPoint_bytes != null)
                                            {
                                              

                                                byte[] command = [cmdRW.ID_WEBAPP,
                                                cmdRW.ID_MODULO_BASE,
                                                cmdRW.ID_MODULO_BASE,
                                                cmdRW.RESPONSE_CMD1,
                                                cmdRW.SAVE_MISSION_CMD2,
                                                cmdRW.SAVE_MISSION_WP_CMD3];

                                                byte[] newCommand = SendConstructBuff(command, wayPoint_bytes);

                                                ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("new_internal_data", newCommand));
                                                //return Task.FromResult(new SignalBusMessage("new_internal_data", newCommand));
                                                

                                            }

                                            if (nWP_now == wayPoints.Count)
                                            {
                                                wayPoints = null;
                                                nWP_now = 0;
                                            }

                                        }
                                        else
                                        {
                                            wayPoints = null;
                                            nWP_now = 0;
                                        }
                                        
                                    }
                                    

                                    //CostructBuff()
                                    /* combinaVar(&nWP_now, sizeof(nWP_now), bufferAnal);
                                     qDebug() << nWP_now;
                                     costructBuff(&wayPointData[nWP_now], sizeof(wayPointData[nWP_now]));
                                     sendCostructBuff(port, myID, ID_MODULO_BASE, ID_MODULO_BASE, RESPONSE_CMD1, mes.Cmd2, mes.Cmd3);*/
                                }
                            }
                        }
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.GET_IP_PORT_CMD2)
                            {
                            }

                            if (_teensyMessage.Cmd2 == cmdRW.UPDATE_MISS_LIST_CMD2)
                            {


                                

                                UInt16 fileCount = 0;
                                UInt16 index_buff = 0;

                                int index_counter_buffer = 0;

                                Tuple<UInt16, int> fileCount_result = CombinaMultiVar<UInt16>(index_counter_buffer, bufferDataOnly, fileCount);
                                fileCount = fileCount_result.Item1;
                                index_counter_buffer = fileCount_result.Item2;

                                Tuple<UInt16, int> index_buff_result = CombinaMultiVar<UInt16>(index_counter_buffer, bufferDataOnly, index_buff);
                                index_buff = index_buff_result.Item1;
                                
                                
                                byte[] array_data = new byte[bufferDataOnly.Length - Marshal.SizeOf(fileCount) * 2];
                                //array_data = bufferDataOnly.Skip(Marshal.SizeOf(fileCount) * 2).ToArray();

                                Array.Copy(bufferDataOnly, Marshal.SizeOf(fileCount * 2), array_data, 0, array_data.Length);

                                string fileName = "";
                                byte fileNameIndex = 0;


                                DirectoryNode root = new DirectoryNode("RootsBloodyRoots");
                                Stack<DirectoryNode> directoryStack = new Stack<DirectoryNode>();
                                directoryStack.Push(root);

                                for (byte k = 0; k < index_buff; k++)
                                {
                                    char byteNow = (char)array_data[k];


                                    if (byteNow == cmdRW.DIRECTORY_CMD)
                                    {
                                        Debug.WriteLine("Creating directory: " + fileName);
                                        DirectoryNode newDirectory = new DirectoryNode(fileName);
                                        directoryStack.Peek().AddNode(newDirectory);
                                        directoryStack.Push(newDirectory);
                                        fileName = "";
                                        fileNameIndex = 0;
                                    }
                                    else if (byteNow == cmdRW.FILE_CMD)
                                    {
                                        Debug.WriteLine("Creating file: " + fileName);
                                        directoryStack.Peek().AddNode(new FileNode(fileName));
                                        fileName = "";
                                        fileNameIndex = 0;
                                    }
                                    else if (byteNow == cmdRW.PARENT_CMD)
                                    {
                                        Debug.WriteLine("Moving up to parent directory");
                                        if (directoryStack.Count > 1)
                                        {
                                            directoryStack.Pop();
                                        }
                                        fileName = "";
                                        fileNameIndex = 0;
                                    }
                                    else
                                    {
                                        if (byteNow == 0)
                                        {
                                 
                                        }else
                                        {
                                            fileName += byteNow;
                                            fileNameIndex++;
                                        }
                                        
                                    }
                                }

                                if (_teensyMessage.Cmd3 == cmdRW.UPDATE_FILE_LIST_CMD3)
                                {
                                    // byte[] fileCountArr = BitConverter.GetBytes(fileCount);

                                    // bool succ = CostructBuff(fileCountArr);

                                    // if (succ)
                                    // {
                                    //     //here change
                                    //     //SendCostructBuff(cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.UPDATE_MISS_LIST_CMD2, cmdRW.UPDATE_FILE_LIST_CMD3);
                                        
                                    // }
                                    throw new NotImplementedException();

                                }else
                                {
                                    string json = JsonConvert.SerializeObject(root, Formatting.Indented);
                                    ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("DTree", null, json));
                                    // return Task.FromResult(new SignalBusMessage("DTree", null, json));

                                }

                                
                                //HANDLE HERE IF STRUCTURE IS BIGGER THAN 255 BYTES
                                //HANDLE DIFFERENT FROM WHAT THEY DID SO INSTANCE DOES NOT RETURN PARTIAL DATA
                                //TO CHECK FIX AND IMPLEMENT
                                /*if (_teensyMessage.Cmd3 == cmdRW.UPDATE_FILE_LIST_CMD3)
                                {
                                    byte[] fileCountArr = BitConverter.GetBytes(fileCount);

                                    bool succ = CostructBuff(fileCountArr);

                                    if (succ)
                                    {
                                        //here change
                                        //SendCostructBuff(cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.UPDATE_MISS_LIST_CMD2, cmdRW.UPDATE_FILE_LIST_CMD3);
                                        
                                    }

                                }*/
                                
                               // return Task.FromResult(new SignalBusMessage("DTree", null, false, json));

                            }


                            if (_teensyMessage.Cmd2 == cmdRW.GET_MISSION_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.GET_MISSION_PARAM_CMD3)
                                {
                                    
                                    MissionParamIn missionParameters_in = CombinaVar<MissionParamIn>(bufferDataOnly);

                                    MissionParam missionParameters = new MissionParam
                                    {
                                        nMission = missionParameters_in.nMission,
                                        total_mission_nWP = missionParameters_in.total_mission_nWP,
                                        standRadius = missionParameters_in.standRadius,
                                        NMStartInd = missionParameters_in.NMStartInd,
                                        cycles = missionParameters_in.cycles,
                                        NMmode = missionParameters_in.NMmode,
                                        NMnum = missionParameters_in.NMnum,
                                        wpEnd = missionParameters_in.wpEnd,
                                        wpStart = missionParameters_in.wpStart
                                    };

                                    for (int i = 0; i < missionParameters_in.idMission.Length; i++)
                                    {
                                        char byteNow = (char)missionParameters_in.idMission[i];

                                        if (byteNow == '\0')
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            missionParameters.idMission += byteNow;
                                        }
                                    }

                                    for (int i = 0; i < missionParameters_in.idMissionNext.Length; i++)
                                    {
                                        char byteNow = (char)missionParameters_in.idMissionNext[i];

                                        if (byteNow == '\0')
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            missionParameters.idMissionNext += byteNow;
                                        }
                                    }

                                    totalWaypoints = missionParameters.total_mission_nWP;

                                    string json = JsonConvert.SerializeObject(missionParameters, Formatting.Indented);
                                    
                                    //GotNewData("MMW", json);

                                    wayPoints = new List<WayPoint>();

                                    nWP_now = 0;
                                    byte[] wpbyte = BitConverter.GetBytes(nWP_now);

                                    byte[] newCommand = SendConstructBuff([cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.GET_MISSION_CMD2, cmdRW.GET_MISSION_WP_CMD3], wpbyte);
                                    
                                    
                                    ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("MMW", newCommand, json));
                                    //return Task.FromResult();
                                    

                                    //return Task.FromResult(new SignalBusMessage() { data_in = SeriaDataAndReturn("MMW", json),  data_command = newCommand, NeedAnswer = true });


                                    //eturn Task.FromResult(new NeedData() { taskNeedResult = true, commandData = newCommand, NeedPreparation = false});

                                }

                                if (_teensyMessage.Cmd3 == cmdRW.GET_MISSION_WP_CMD3)
                                {
                                    if (nWP_now < totalWaypoints)
                                    {
                                        
                                        WayPoint onewaypoint = CombinaVar<WayPoint>(bufferDataOnly);

                                        wayPoints.Add(onewaypoint);

                                        nWP_now++;

                                        if (nWP_now < totalWaypoints)
                                        {
                                            byte[] wpbyte = BitConverter.GetBytes(nWP_now);
                                            //Array.Reverse(wpbyte);
                                                
                                            
                                            byte[] newCommand = SendConstructBuff([cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.GET_MISSION_CMD2, cmdRW.GET_MISSION_WP_CMD3], wpbyte);
                                            
                                            ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("new_internal_data", newCommand));
                                            //return Task.FromResult();
                                            
                                        }
                                        else
                                        {
                                            string json = JsonConvert.SerializeObject(wayPoints, Formatting.Indented);
                                            totalWaypoints = 0;

                                            ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("AllWayPoints", null, json));
                                            //return Task.FromResult();
                                            //return Task.FromResult(new SignalBusMessage() { data_in = SeriaDataAndReturn("AllWayPoints", json), data_command = null, NeedAnswer = true });
                                        }

                                    }
                                }

                            }

                            //this if for cmd2 is redundant
                            /*if (_teensyMessage.Cmd2 == cmdRW.GET_MISSION_CMD2)
                            {
                               
                            }*/
                            
                            if (_teensyMessage.Cmd2 == cmdRW.SEND_DUMMY_CMD2)
                            {

                            }

                            if (_teensyMessage.Cmd2 == cmdRW.RADIO_DRIVING_DATA_CMD2)
                            {

                            }

                            if (_teensyMessage.Cmd2 == cmdRW.GET_CONTROL_INFO_CMD2)
                            {

                            }
                        }

                        break;

                    case cmdRW.ID_ALTO_LIVELLO:

                        break;

                    case cmdRW.ID_INTERFACCIA:

                        break;

                    case cmdRW.ID_RADIOCOMANDO:

                        break;

                    case cmdRW.ID_MODULO_AMB:

                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.UPDATE_MISS_LIST_CMD2)
                            {


                                if (_teensyMessage.Cmd3 == cmdRW.UPDATE_FILE_LIST_CMD3)
                                {

                                }
                            }
                        }

                        break;

                    case cmdRW.ID_ROBOT_ARM_1:
                    case cmdRW.ID_ROBOT_ARM_2:

                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.SERVO_GET_PER_CMD2)
                            {

                            }
                            if (_teensyMessage.Cmd2 == cmdRW.SERVO_GET_ANG_CMD2)
                            {

                            }
                        }

                        break;

                    case cmdRW.ID_PRUA:

                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.GET_PRESSURE_CMD2)
                            {

                            }
                        }

                        break;

                    case cmdRW.ID_IMU:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if ((_teensyMessage.Cmd2 == cmdRW.IMU_GET_CMD2) && (_teensyMessage.Cmd3 == cmdRW.IMU_RPY_ACC_CMD3))
                            {

                                if(bufferDataOnly.Length > 0)
                                {
                                    ImuData imuData = CombinaVar<ImuData>(bufferDataOnly);
                                   
                                    string json_data = JsonConvert.SerializeObject(imuData);

                                    ecodroneBoat.signalBusSocket.Publish(new SignalBusMessage("ImuData", null, json_data));
                                    //return Task.FromResult();
                                }
                                

                            }

                            if (_teensyMessage.Cmd2 == cmdRW.IMU_CFG_CMD2)
                            {
                                switch (_teensyMessage.Cmd3)
                                {
                                    case cmdRW.IMU_UPDATE_CFG_GET_CMD3:


                                        break;

                                    case cmdRW.IMU_UPDATE_CFG_SET_CMD3:

                                        break;

                                    case cmdRW.IMU_UPDATE_CAL_GET_CMD3:

                                        break;

                                    case cmdRW.IMU_UPDATE_CAL_SET_CMD3:

                                        break;
                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.IMU_DEB_CFG_CMD2)
                            {
                                switch (_teensyMessage.Cmd3)
                                {
                                    case cmdRW.IMU_UPDATE_CFG_GET_CMD3:
                                        break;
                                }
                            }
                        }
                        break;

                    case cmdRW.ID_MOTORI:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.MOTOR_TELEM_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.MOTOR_TELEM_CDCS_CMD3)
                                {

                                }
                                if (_teensyMessage.Cmd3 == cmdRW.MOTOR_TELEM_DDSS_CMD3)
                                {

                                }
                            }
                        }
                        break;

                    case cmdRW.ID_BMS:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {

                            if (_teensyMessage.Cmd2 == cmdRW.BMS_PARAM_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.BMS_GET_PARAM_CMD3)
                                {

                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.BMS_GET_DATA_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.BMS_GET_VCELL_CMD3)
                                {

                                }

                                if (_teensyMessage.Cmd3 == cmdRW.BMS_GET_BASIC_CMD3)
                                {

                                }
                                if (_teensyMessage.Cmd3 == cmdRW.BMS_GET_EEPROM_CMD3)
                                {


                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.BMS_DEB_CFG_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.BMS_DEB_CFG_GET_CMD3)
                                {

                                }
                            }
                        }

                        break;

                    case cmdRW.ID_GPS:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.GPS_GET_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.GPS_NAV_PVT_CMD3)
                                {


                                }
                                if (_teensyMessage.Cmd3 == cmdRW.GPS_NAV_RELPOSNED_CMD3)
                                {

                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.GPS_DEB_CFG_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.GPS_DEB_CFG_GET_CMD3)
                                {

                                }
                            }
                        }
                        break;

                    case cmdRW.ID_ECHO:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.ECHO_GET_CMD2)
                            {

                            }

                            if (_teensyMessage.Cmd2 == cmdRW.ECHO_CFG_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.ECHO_CFG_GET_CMD3)
                                {

                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.ECHO_DEB_CFG_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.ECHO_DEB_CFG_GET_CMD3)
                                {

                                }
                            }
                        }
                        break;

                    case cmdRW.ID_LED:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.LIGHT_SENS_CMD2)
                            {

                            }
                        }
                        break;

                    case cmdRW.ID_MPPT:
                        if (_teensyMessage.Cmd1 == cmdRW.RESPONSE_CMD1)
                        {
                            if (_teensyMessage.Cmd2 == cmdRW.MPPT_DEB_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.MPPT_DEB_GET_CMD3)
                                {


                                }
                            }
                            if (_teensyMessage.Cmd2 == cmdRW.MPPT_GET_CMD2)
                            {

                            }
                        }
                        break;
                }
            }

        }

    }

}




class FileNode
{
    public string Type { get; } = "file";
    public string Name { get; }

    public FileNode(string name)
    {
        Name = name;
    }
}

// Class to represent a directory node in the directory structure
class DirectoryNode
{
    public string Type { get; } = "directory";
    public string Name { get; }
    public List<object> Children { get; } = new List<object>();

    public DirectoryNode(string name, params object[] children)
    {
        Name = name;
        Children.AddRange(children);
    }

    public void AddNode(object node)
    {
        Children.Add(node);
    }
}