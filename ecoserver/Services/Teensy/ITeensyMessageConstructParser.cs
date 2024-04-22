using System;
using System.Net.Sockets;

namespace webapi
{
    public interface ITeensyMessageConstructParser
    {
        //byte[] PrepareTeensyRequest(byte[] c);
        void ParseTeensyMessage(byte[] arra_of_data_byte);
        void UpdateListWayPoints(List<WayPoint> wayPoints);
        byte[] SendConstructBuff(byte[] command,  byte[]? object_array);
        // byte[] ConstructBuff(byte[] obj);
        
    }
}
