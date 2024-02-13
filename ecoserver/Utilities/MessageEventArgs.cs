using Newtonsoft.Json;
using System.Collections;
using System.Net.WebSockets;

namespace webapi
{
    public class TMS
    {
        public string MessageId { get; set; } = "";
        public string MessageData { get; set; } = "";
        public string MessageType { get; set; } = "";
    }

    public class MessageContainerClass
    {
        public string IdContainer { get; }
        public byte[] CommandId { get; }
        public bool NeedPreparation { get; }

        public MessageContainerClass(string idContainer, byte[] commandId, bool needPreparation = true)
        {
            IdContainer = idContainer;
            CommandId = commandId;
            NeedPreparation = needPreparation;
        }
    }
}
