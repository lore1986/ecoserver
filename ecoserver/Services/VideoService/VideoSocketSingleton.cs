using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Routing.Constraints;
using System.Net.Sockets;

namespace webapi.Services.SocketService
{

    public class VideoSocketSingleton : IVideoSocketSingleton
    {
        private readonly ILogger<VideoSocketSingleton> logger;
        private List<VideoTcpListener> groups = new List<VideoTcpListener>();


        public VideoSocketSingleton(ILogger<VideoSocketSingleton> _logger) 
        { 
            logger = _logger;
        }

        public Tuple<int, VideoTcpListener?> CreateVideoTcpListener(string groupid)
        {
            int indexGroup = -1;

            if(!groups.Any(x => x.group_id == groupid))
            {
                VideoTcpListener group = new VideoTcpListener
                {
                    group_id = groupid
                };
                groups.Add(group);

                indexGroup = groups.IndexOf(group);

                return new Tuple<int, VideoTcpListener?>(indexGroup, group);
            }

            return new Tuple<int, VideoTcpListener?>(-1, null);
        }


        


    }
}
