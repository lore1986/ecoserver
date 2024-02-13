namespace webapi.Services.BusService
{
    public interface IBusEvent
    {
        void Publish(BusEventMessage eventMessage);
        void Unsubscribe(Action<BusEventMessage> subscriber);
        void Subscribe(Action<BusEventMessage> subscriber);

    }
}
