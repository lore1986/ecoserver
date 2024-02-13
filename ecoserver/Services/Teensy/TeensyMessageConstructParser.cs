using Newtonsoft.Json;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using webapi;
using webapi;



//using webapi.Controllers;
using webapi.Utilities;
using static webapi.ITeensyMessageConstructParser;


//8 OClock commit

namespace webapi
{

    public class TeensyMessageConstructParser : ITeensyMessageConstructParser
    {

        private cmdRW _cmdRW  { get; set; }
        private ParamId _paramId { get; set; }
        public string _url { get; private set; } = string.Empty;
        public int _port { get; private set; } = 0;

        
        private byte[] bufferSend = new byte[255];


        private List<WayPoint>? wayPoints = null;
        private UInt16 nWP_now { get; set; } = 0;
        private UInt16 totalWaypoints { get; set; } = 0;


        private ushort initMultiVar { get; set; } = 0;
        private ushort indexByteLoad { get; set; } = 10;


        public TeensyMessageConstructParser()
        {
            _paramId = new ParamId();
            _cmdRW = new cmdRW();
        }



        public void UpdateListWayPoints(object sender, List<WayPoint> wayPoints)
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
            TMS newTms = new TMS();
            newTms.MessageType = idData;
            newTms.MessageData = data;

            string json_data = System.Text.Json.JsonSerializer.Serialize(newTms);
            byte[] _dataSend = Encoding.UTF8.GetBytes(json_data);
            return _dataSend;
        }



        public byte[] PrepareTeensyRequest(byte[] c)
        {
            if(c.Length > 6)
            {
                byte[] payload = new byte[c.Length - 6];
                Array.Copy(c, 6, payload, 0, payload.Length);

                CostructBuff(payload);
            }

            byte[] commands = new byte[6];
            Array.Copy(c, 0, commands, 0, 6);

            byte[] buffer_ready_send = SendCostructBuff(commands);

            indexByteLoad = cmdRW.INDEX_BUF_CONTB;

            //ABSOULUTELY CHECK THIS
            initMultiVar = 0;

            return buffer_ready_send;
        }



        public async Task<ChannelTeensyMessage> ReadBufferAsync(NetworkStream? nS)
        {
            if(nS == null)
            {
                return new ChannelTeensyMessage();
            }

            byte[] dataread = new byte[255];
            int bytesRead = await nS.ReadAsync(dataread);

            byte[] bufferRead = new byte[255];
            byte buff_step = 0;
            byte message_length = 0;
            byte indexByte = 0;
            string my_buff_text = "";

            for (int i = 0; i < bytesRead; i++)
            {
                my_buff_text += string.Concat(dataread[i].ToString(), ",");


                switch (buff_step)
                {
                    case 0:
                        if (dataread[i] == _paramId.Boat[0])
                        {
                            buff_step++;
                        }
                        else
                        {
                            buff_step = 0;

                            //char myChar = Convert.ToChar(dataread);

                        }
                        break;

                    case 1:
                        if (dataread[i] == _paramId.Boat[1])
                        {
                            buff_step++;
                        }
                        else
                        {
                            buff_step = 0;
                        }
                        break;
                    case 2:
                        if (dataread[i] == _paramId.Boat[2])
                        {
                            buff_step++;
                        }
                        else
                        {
                            buff_step = 0;
                        }
                        break;

                    case 3:

                        message_length = dataread[i];
                        bufferRead = new byte[message_length];
                        indexByte = 0;
                        bufferRead[indexByte] = dataread[i];
                        buff_step++;

                        break;

                    case 4:
                        indexByte++;
                        bufferRead[indexByte] = dataread[i];

                        ChannelTeensyMessage teensyMessage =  new ChannelTeensyMessage();

                        if (indexByte >= message_length - 1)
                        {
                            if (cksumTest(bufferRead))
                            {
                                teensyMessage =  await analBuff(bufferRead);
                            }
                            else
                            {
                                Debug.WriteLine("error");
                            }
                            
                            
                            return teensyMessage;

                        }
                        break;
                }
            }

            return new ChannelTeensyMessage();
        }

        private byte CksumCompute(byte[] buff)
        {
            byte cksum = 0;
            int indexBufLeng = 3; // Assuming INDEX_BUF_LENG is 3

            var t = buff[indexBufLeng];

            for (int i = indexBufLeng; i < (buff[indexBufLeng] + indexBufLeng - 1); i++)
            {
                cksum += buff[i];
            }

            return cksum;
        }

        private byte[] SendCostructBuff(byte[] command)
        {
            bufferSend[cmdRW.INDEX_SINCHAR_0] = _paramId.Boat[0];
            bufferSend[cmdRW.INDEX_SINCHAR_1] = _paramId.Boat[1];
            bufferSend[cmdRW.INDEX_SINCHAR_2] = _paramId.Boat[2];

            bufferSend[cmdRW.INDEX_BUF_LENG] = (byte)(indexByteLoad - cmdRW.INDEX_SINCHAR_2);
            bufferSend[cmdRW.INDEX_BUF_SORG] = command[0];
            bufferSend[cmdRW.INDEX_BUF_DEST] = command[1];
            bufferSend[cmdRW.INDEX_BUF_ID_D] = command[2];

            bufferSend[cmdRW.INDEX_BUF_CMD_1] = command[3];
            bufferSend[cmdRW.INDEX_BUF_CMD_2] = command[4];
            bufferSend[cmdRW.INDEX_BUF_CMD_3] = command[5];


            bufferSend[indexByteLoad] = CksumCompute(bufferSend);

            byte[] array1 = new byte[indexByteLoad + 1];

            for (ushort i = 0; i < indexByteLoad + 1; i++)
            {
                array1[i] = bufferSend[i];
            }

            indexByteLoad = cmdRW.INDEX_BUF_CONTB;

            return array1;
        }

        private bool CostructBuff(byte[] obj)
        {
            if (obj == null)
            {
                return false;
            }

            long length_buffer = cmdRW.INDEX_BUF_CONTB + obj.Length + 1;
            bufferSend = new byte[length_buffer];

            for (ushort i = 0; i < obj.Length; i++)
            {
                bufferSend[indexByteLoad] = obj[i];
                indexByteLoad++;
            }

            initMultiVar = cmdRW.INDEX_BUF_CONTB - cmdRW.INDEX_BUF_LENG;

            return true;

        }

        private byte[]? ToBytesArray(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            using (MemoryStream streamN = new MemoryStream())
            {
                System.Text.Json.JsonSerializer.SerializeAsync(streamN, obj, obj.GetType()).Wait();

                streamN.Position = 0;
                long streamSize = streamN.Length;

                long length_buffer = cmdRW.INDEX_BUF_CONTB + streamN.Length + 1;

                bufferSend = new byte[length_buffer];

                return streamN.ToArray();

               /* await JsonSerializer.SerializeAsync(streamN, obj, obj.GetType());

                // Position is not needed, as the next line already considers the current position
                long streamSize = streamN.Length;

                long length_buffer = cmdRW.INDEX_BUF_CONTB + streamN.Length + 1;

                bufferSend = new byte[length_buffer];

                byte[] buffer_temp = streamN.ToArray();
                Buffer.BlockCopy(buffer_temp, 0, bufferSend, indexByteLoad, buffer_temp.Length);
                indexByteLoad += buffer_temp.Length;*/
            }

/*            initMultiVar = cmdRW.INDEX_BUF_CONTB - cmdRW.INDEX_BUF_LENG;*/

        }

        private T CombinaVar<T>(byte[] bytes_convert)
        {
            T _template = default(T);

            GCHandle gcHandle = GCHandle.Alloc(bytes_convert, GCHandleType.Pinned);
            _template = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();

            return (T)_template;
        }

        private T CombinaMultiVar<T>(byte[] buff_extract, object object_template)
        {

            T _template = default(T);

            long buff_size = buff_size = Marshal.SizeOf(object_template.GetType());
            
            byte[] buff_object = new byte[buff_size];

            for (UInt16 i = 0; i < buff_size; i++)
            {
                buff_object[i] = buff_extract[initMultiVar];
                initMultiVar++;
            }

            GCHandle gcHandle = GCHandle.Alloc(buff_object, GCHandleType.Pinned);
            _template = (T)Marshal.PtrToStructure(gcHandle.AddrOfPinnedObject(), typeof(T));
            gcHandle.Free();

            return _template;
            
        }

        private byte[] CombinaMultiVarArr(byte[] buff_extract)
        {

            //byte[] buff_object = new byte[buff_extract.Length - 12];

            for (byte i = 0; i < buff_extract.Length; i++)
            {
                //buff_extract[i] = buff_extract[initMultiVar];
                initMultiVar++;
            }

            return buff_extract;
        }

        void EndCombinaMultiVar()
        {
            initMultiVar = cmdRW.INDEX_BUF_CONTB - cmdRW.INDEX_BUF_LENG;
        }

        private bool cksumTest(byte[] buff)
        {
            byte cksum = 0;

            for (byte i = 0; i < (buff[0] - 1); i++)
            {
                cksum += buff[i];

            }

            if (cksum == buff[(buff[0] - 1)])
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
        /*private int CopyToByteArray(byte[] source, byte[] destination, int startIndex)
        {
            Array.Copy(source, 0, destination, startIndex, source.Length);
            return startIndex + source.Length;
        }*/

        private Task<ChannelTeensyMessage> analBuff(byte[] mybuffdata)
        {
            TeensyMessage _teensyMessage = new TeensyMessage(mybuffdata);

            byte[] bufferDataOnly = BufferDataOnly(mybuffdata);

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
                                                CostructBuff(wayPoint_bytes);

                                                byte[] command = [cmdRW.ID_WEBAPP,
                                                    cmdRW.ID_MODULO_BASE,
                                                    cmdRW.ID_MODULO_BASE,
                                                    cmdRW.RESPONSE_CMD1,
                                                    cmdRW.SAVE_MISSION_CMD2,
                                                    cmdRW.SAVE_MISSION_WP_CMD3];

                                                byte[] newCommand = SendCostructBuff(command);

                                                return Task.FromResult(new ChannelTeensyMessage() { data_in = null, data_command = newCommand, NeedAnswer = true });
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


                                byte[] array_data = new byte[255];

                                UInt16 fileCount = 0;
                                UInt16 index_buff = 0;

                                fileCount =  CombinaMultiVar<UInt16>(bufferDataOnly, fileCount);
                                index_buff = CombinaMultiVar<UInt16>(bufferDataOnly, index_buff);
                                array_data = CombinaMultiVarArr(bufferDataOnly.Skip((Marshal.SizeOf(fileCount) * 2)).ToArray());


                            
                                EndCombinaMultiVar();

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


                                string json = JsonConvert.SerializeObject(root, Formatting.Indented);
                                byte[] _dataSend = SeriaDataAndReturn("DTree", json);

                                //return Task.FromResult(new NeedData());


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

                                return Task.FromResult(new ChannelTeensyMessage() { data_in = _dataSend, NeedPreparation = true });

                            }


                            if (_teensyMessage.Cmd2 == cmdRW.GET_MISSION_CMD2)
                            {
                                if (_teensyMessage.Cmd3 == cmdRW.GET_MISSION_PARAM_CMD3)
                                {
                                    
                                    MissionParamIn missionParameters_in = CombinaVar<MissionParamIn>(bufferDataOnly);

                                    MissionParam missionParameters = new MissionParam();

                                    missionParameters.nMission = missionParameters_in.nMission;
                                    missionParameters.total_mission_nWP = missionParameters_in.total_mission_nWP;
                                    missionParameters.standRadius = missionParameters_in.standRadius;
                                    missionParameters.NMStartInd = missionParameters_in.NMStartInd;
                                    missionParameters.cycles = missionParameters_in.cycles;
                                    missionParameters.NMmode = missionParameters_in.NMmode;
                                    missionParameters.NMnum = missionParameters_in.NMnum;
                                    missionParameters.wpEnd = missionParameters_in.wpEnd;
                                    missionParameters.wpStart = missionParameters_in.wpStart;

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

                                    CostructBuff(wpbyte);
                                    byte[] newCommand = SendCostructBuff([cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.GET_MISSION_CMD2, cmdRW.GET_MISSION_WP_CMD3]);

                                    
                                    return Task.FromResult(new ChannelTeensyMessage() { data_in = SeriaDataAndReturn("MMW", json),  data_command = newCommand, NeedAnswer = true });

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
                                            CostructBuff(wpbyte);
                                            byte[] newCommand = SendCostructBuff([cmdRW.ID_WEBAPP, cmdRW.ID_MODULO_BASE, cmdRW.ID_MODULO_BASE, cmdRW.REQUEST_CMD1, cmdRW.GET_MISSION_CMD2, cmdRW.GET_MISSION_WP_CMD3]);

                                            return Task.FromResult(new ChannelTeensyMessage() { data_in = null, data_command = newCommand, NeedAnswer = true });

                                        }
                                        else
                                        {
                                            string json = JsonConvert.SerializeObject(wayPoints, Formatting.Indented);
                                            totalWaypoints = 0;

                                            return Task.FromResult(new ChannelTeensyMessage() { data_in = SeriaDataAndReturn("AllWayPoints", json), data_command = null, NeedAnswer = true });
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
                                ImuData imuData = CombinaVar<ImuData>(bufferDataOnly);


                                byte[] _dataSend = SeriaDataAndReturn("ImuData", System.Text.Json.JsonSerializer.Serialize(imuData));

                                return Task.FromResult(new ChannelTeensyMessage() { data_in = _dataSend, NeedPreparation = true});

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

            return Task.FromResult(new ChannelTeensyMessage()); 
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