using System.Security.Cryptography.X509Certificates;
using webapi;
using webapi.Utilities;

public class TMS
{
    public string MessageId { get; set; } = "";
    public string MessageData { get; set; } = "";
    public string MessageType { get; set; } = "";
}

public class TeensyMessageContainer
{
    public string? IdClient {get; set;}
    public string IdContainer { get; }
    public byte[] CommandId { get; }

    public TeensyMessageContainer(string idContainer, byte[] commandId, string? idclient = null)
    {
        IdContainer = idContainer;
        CommandId = commandId;
        IdClient = idclient;
    }
}

public class SignalBusMessage
{
    public string message_id {get; set;} = "NNN";
    public byte[]? data_command { get; set; } = null;
    public string data_message {get; set;}
    public SignalBusMessage( string id_message, byte[]? _newCommand = null, string message_data = "NNN")
    {
        message_id = id_message;
        data_command = _newCommand;
        data_message = message_data;
    }
}

public class EcodroneMessagesContainers
{
    private readonly ITeensyMessageConstructParser teensyparser;
    public readonly List<string> messagesForAll;

    public EcodroneMessagesContainers(ITeensyMessageConstructParser parser_teensy)
    {
        teensyparser = parser_teensy;
        messagesForAll = new List<string>()
        {
            "ImuData"
        };

    }
    public ClientCommunicationStates CheckAllowedContainer(string data_id)
    {
        Dictionary<ClientCommunicationStates, List<string>> allowed_containers = new Dictionary<ClientCommunicationStates, List<string>>
        {
            { ClientCommunicationStates.MISSIONS, new List<string>() { "DTree", "MMW", "AllWayPoints" } },
            { ClientCommunicationStates.SENSORS_DATA, new List<string>() { "ImuData" } },
            { ClientCommunicationStates.WAYPOINT, new List<string>() { "UpMission" } },
            { ClientCommunicationStates.NAVIGATION, new List<string>() {"NavStart"}}
            
        };  

        return allowed_containers.SingleOrDefault(x => x.Value.Contains(data_id)).Key;
    }


    static byte[] ConvertIntArrayToByteArray(int[] state)
    {
        int size = state.Length * sizeof(int); // Calculate the total size of the byte array
        byte[] result = new byte[size];

        for (int i = 0; i < state.Length; i++)
        {
            byte[] bytes = BitConverter.GetBytes(state[i]);
            Array.Copy(bytes, 0, result, i * sizeof(int), bytes.Length);
        }

        return result;
    }
    
    public List<TeensyMessageContainer> GenerateRequestFunct()
    {
        
        List<TeensyMessageContainer> _containers_message = new List<TeensyMessageContainer>();
        
        _ = new cmdRW();

        byte[] imuCommand = teensyparser.SendConstructBuff([
            cmdRW.ID_WEBAPP,
            cmdRW.ID_MODULO_BASE,
            cmdRW.ID_IMU,
            cmdRW.REQUEST_CMD1,
            cmdRW.IMU_GET_CMD2,
            cmdRW.IMU_RPY_ACC_CMD3
        ], null);

        TeensyMessageContainer imuMessage = new TeensyMessageContainer("ImuData", imuCommand);
        _containers_message.Add(imuMessage);

        // int[] state = { 1, 1, 1 };
        // byte[] commandData = ConvertIntArrayToByteArray(state);
        
        // byte[] command =  
        // {
        //     1, 44, 44, 0 , 0 , 0
        // };
        
        // byte[] newCommand = teensyparser.SendConstructBuff(command, commandData);
        // TeensyMessageContainer newCommandmessage = new TeensyMessageContainer("AllOn", newCommand);
        // _containers_message.Add(newCommandmessage);
        


        return _containers_message;
    }
}