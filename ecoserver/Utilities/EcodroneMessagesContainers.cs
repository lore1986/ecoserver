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

    public TeensyMessageContainer(string idContainer, byte[] commandId, bool needPreparation = true)
    {
        IdContainer = idContainer;
        CommandId = commandId;
        NeedPreparation = needPreparation;
    }
}

public class ChannelTeensyMessage
{
    //data parsed
    //public byte[]? data_in { get; set; } = null;
    //data when command needs ping pong
    public string message_id {get; set;} = "NNN";
    public byte[]? data_command { get; set; } = null;
    //needs ping pong? just confirm it's a double check
    public bool needAnswer { get; set; } = false;
    public string data_message {get; set;}
    public ChannelTeensyMessage(string id_message, byte[]? _newCommand = null, bool _needAnswer = false, string message_data = "NNN")
    {
        message_id = id_message;
        data_command = _newCommand;
        needAnswer = _needAnswer;
        data_message = message_data;
    }
}

public static class EcodroneMessagesContainers
{
    public static List<TeensyMessageContainer> GenerateRequestFunct()
    {
        List<TeensyMessageContainer> _containers_message = new List<TeensyMessageContainer>();
        
        _ = new cmdRW();

        TeensyMessageContainer imuMessage = new TeensyMessageContainer("ImuData",
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