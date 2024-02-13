namespace webapi
{
    public class VideoBusService : IVideoBusService
    {
        private readonly List<Action<VideoMessage>> _subscribers = new List<Action<VideoMessage>>();

        public void Subscribe(Action<VideoMessage> subscriber)
        {
            _subscribers.Add(subscriber);  
        }

        public void Unsubscribe(Action<VideoMessage> unsubscriber)
        {
            _subscribers.Remove(unsubscriber);
        }

        public void Publish(VideoMessage eventMessage)
        {
            foreach (var sub in _subscribers)
            {
                if(sub != null)
                {
                    sub.Invoke(eventMessage);
                }
            }
        }

    }
}

   