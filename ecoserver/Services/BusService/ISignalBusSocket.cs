using System.Diagnostics;

namespace webapi
{
    public interface ISignalBusSocket
    {
        public void Subscribe(Action<ChannelTeensyMessage> action, EcoClient client);

        public void Unsubscribe(Action<ChannelTeensyMessage> action, EcoClient client);

        public void Publish(ChannelTeensyMessage eventMessage);

        public bool IsASubscriber(string id);

    }
}

   