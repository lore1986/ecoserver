namespace webapi
{ 
    public interface IVideoBusService
    {
        void Subscribe(Action<VideoMessage> subscriber);

        void Unsubscribe(Action<VideoMessage> unsubscriber);

        void Publish(VideoMessage eventMessage);

    }
}
