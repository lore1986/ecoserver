using System.Net.Sockets;

namespace webapi
{ 
    public interface IVideoSocketSingleton
    {
        Tuple<int, VideoTcpListener?> CreateVideoTcpListener(string groupid);
        //Tuple<int, VideoListener?> CreateAddClient(string groupid, string uuid, Socket socket);
    }
}
