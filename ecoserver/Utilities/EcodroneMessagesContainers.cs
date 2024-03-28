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
    public string IdContainer { get; }
    public byte[] CommandId { get; }
    public bool NeedPreparation { get; }
    public string IdClient {get; set;}

    public TeensyMessageContainer(string id_client, string idContainer, byte[] commandId, bool needPreparation = true)
    {
        IdContainer = idContainer;
        CommandId = commandId;
        NeedPreparation = needPreparation;
        IdClient = id_client;
    }
}

public class ChannelTeensyMessage
{
    public string id_client {get; set;}
    public string message_id {get; set;} = "NNN";
    public byte[]? data_command { get; set; } = null;
    public bool needAnswer { get; set; } = false;
    public string data_message {get; set;}
    public ChannelTeensyMessage(string _id_client, string id_message, byte[]? _newCommand = null, bool _needAnswer = false, string message_data = "NNN")
    {
        id_client = _id_client;
        message_id = id_message;
        data_command = _newCommand;
        needAnswer = _needAnswer;
        data_message = message_data;
    }
}

public static class EcodroneMessagesContainers
{
    public static ClientCommunicationStates CheckAllowedContainer(string data_id)
    {
        Dictionary<ClientCommunicationStates, List<string>> allowed_containers = new Dictionary<ClientCommunicationStates, List<string>>
        {
            { ClientCommunicationStates.MISSIONS, new List<string>() { "DTree", "MMW", "AllWayPoints" } },
            { ClientCommunicationStates.SENSORS_DATA, new List<string>() { "ImuData" } },
            { ClientCommunicationStates.WAYPOINT, new List<string>() { "UpMission" } }
            
        };  

        return allowed_containers.SingleOrDefault(x => x.Value.Contains(data_id)).Key;
    }

    
    public static List<TeensyMessageContainer> GenerateRequestFunct()
    {
        
        List<TeensyMessageContainer> _containers_message = new List<TeensyMessageContainer>();
        
        _ = new cmdRW();

        TeensyMessageContainer imuMessage = new TeensyMessageContainer("all", "ImuData",
        [
            cmdRW.ID_WEBAPP,
            cmdRW.ID_MODULO_BASE,
            cmdRW.ID_IMU,
            cmdRW.REQUEST_CMD1,
            cmdRW.IMU_GET_CMD2,
            cmdRW.IMU_RPY_ACC_CMD3
        ]);


        _containers_message.Add(imuMessage);


        return _containers_message;
    }
}