using System.Diagnostics;

namespace webapi
{
    public class SignalBusSocket : ISignalBusSocket
    {

        private readonly List<Tuple<Action<ChannelTeensyMessage>, EcoClient>> _subscribers = new List<Tuple<Action<ChannelTeensyMessage>, EcoClient>>();

        public void Subscribe(Action<ChannelTeensyMessage> action, EcoClient client)
        {
            var userTuple = new Tuple<Action<ChannelTeensyMessage>, EcoClient>(action, client);
            _subscribers.Add(userTuple);  
        }

        public void Unsubscribe(Action<ChannelTeensyMessage> action, EcoClient client)
        {
            var userTuple = new Tuple<Action<ChannelTeensyMessage>, EcoClient>(action, client);
            _subscribers.Remove(userTuple);
        }

        public void Publish(ChannelTeensyMessage eventMessage)
        {
            foreach (var sub in _subscribers)
            {
                if(eventMessage.id_client == "all" || eventMessage.id_client == sub.Item2.IdClient)
                {
                    sub.Item1.Invoke(eventMessage);
                }
                
            }
        }

        public bool IsASubscriber(string id)
        {
            try{
                bool tuple = _subscribers.Any(x => x.Item2.IdClient == id);
                return tuple;
            }catch(Exception ex)
            {
                Debug.WriteLine($"error on subscriber: { ex.Message }");
                return false;
            }
            
        }

    }
}

   