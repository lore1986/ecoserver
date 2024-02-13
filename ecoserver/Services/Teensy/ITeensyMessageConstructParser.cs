using System;
using System.Net.Sockets;

namespace webapi
{
    public interface ITeensyMessageConstructParser
    {
        byte[] PrepareTeensyRequest(byte[] c);
        Task<ChannelTeensyMessage> ReadBufferAsync(NetworkStream nS);
        void UpdateListWayPoints(object sender, List<WayPoint> wayPoints);
        
    }
}
