using System.Diagnostics;

namespace webapi
{
    public class SignalBusSocket
    {
        private readonly List<Tuple<Action<ChannelTeensyMessage>, string>> _subscribers = new List<Tuple<Action<ChannelTeensyMessage>, string>>();

        public void Subscribe(Action<ChannelTeensyMessage> action, string id)
        {
            var userTuple = new Tuple<Action<ChannelTeensyMessage>, string>(action, id);
            _subscribers.Add(userTuple);  
        }

        public void Unsubscribe(Action<ChannelTeensyMessage> action, string id)
        {
            var userTuple = new Tuple<Action<ChannelTeensyMessage>, string>(action, id);
            _subscribers.Remove(userTuple);
        }

        public void Publish(ChannelTeensyMessage eventMessage)
        {
            foreach (var sub in _subscribers)
            {
                sub.Item1.Invoke(eventMessage);
            }
        }

        public bool IsASubscriber(string id)
        {
            try{
                bool tuple = _subscribers.Any(x => x.Item2 == id);
                return tuple;
            }catch(Exception ex)
            {
                Debug.WriteLine($"error on subscriber: { ex.Message }");
                return false;
            }
            
        }

    }
}

   