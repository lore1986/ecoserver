namespace webapi
{ 
    public interface IVideoBusService
    {
        void Subscribe(Action<EcodroneBoatMessage> subscriber, string id);

        void Unsubscribe(Action<EcodroneBoatMessage> unsubscriber, string id);

        void Publish(EcodroneBoatMessage eventMessage);
        bool IsASubscriber(string id);
    }
}
