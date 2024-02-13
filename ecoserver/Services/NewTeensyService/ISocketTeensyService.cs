namespace webapi.Services.NewTeensyService
{
    public interface ISocketTeensyService
    {
        ChannelTeensyMessage? ReadOnChannel(string idMaskedTeensy);
        void WriteOnChannel(string mIdTeensy, ChannelTeensyMessage? channelMessage);

        //Events delegates
        void NewClientConnectionEvent(object sender, NewClientEventArgs e);
        void GotCommand(object sender, BusEventMessage busEventMessage);


/*        JetsonSocketHandler? GroupExist(string idGroup);*/

    }
}
