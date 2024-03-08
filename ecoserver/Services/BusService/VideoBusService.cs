namespace webapi
{
    public class VideoBusService : IVideoBusService
    {
        private readonly List<Tuple<Action<EcodroneBoatMessage>, string>> _subscribers = new List<Tuple<Action<EcodroneBoatMessage>, string>>();

        public void Subscribe(Action<EcodroneBoatMessage> action, string id)
        {
            var userTuple = new Tuple<Action<EcodroneBoatMessage>, string>(action, id);
            _subscribers.Add(userTuple);  
        }

        public void Unsubscribe(Action<EcodroneBoatMessage> action, string id)
        {
            var userTuple = new Tuple<Action<EcodroneBoatMessage>, string>(action, id);
            _subscribers.Remove(userTuple);
        }

        public void Publish(EcodroneBoatMessage eventMessage)
        {
            foreach (var sub in _subscribers)
            {
                if(sub.Item2 == eventMessage.direction)
                {
                    sub.Item1.Invoke(eventMessage);
                }
            }
        }

        public bool IsASubscriber(string id)
        {
            return _subscribers.Any(x => x.Item2 == id);
        }

    }
}

   