using System.Diagnostics;

namespace webapi
{
    public interface ISignalBusSocket
    {
        public void Subscribe(Action<SignalBusMessage> action, EcoClient client);

        public void Unsubscribe(Action<SignalBusMessage> action, EcoClient client);

        public void Publish(SignalBusMessage eventMessage, string? idclient = null);

        public bool IsASubscriber(string id);

        public void AddMessageToQueue(EcoClient client, string idcontainer);
        EcoClient? ReturnClientWhoRequested(string idcommand);
        void RemoveClientCommandMessage(string idclient);
        bool IsMessageForAll(string idmessage);
    }
}

   