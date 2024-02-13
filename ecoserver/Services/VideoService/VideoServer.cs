//using Newtonsoft.Json;
using Microsoft.AspNetCore.Routing.Constraints;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace webapi
{
    public class VideoServer : VideoListener
    {
        public async void ReadAndSendJetson(VideoMessage v)
        {
            if (v.direction == uuid)
            {
                if (networkStream != null)
                {
                    try
                    {
                        string message_out = JsonSerializer.Serialize(v);
                        byte[] message = Encoding.UTF8.GetBytes(message_out);
                        await networkStream.WriteAsync(message);
                    }catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }
        }
    }
}
