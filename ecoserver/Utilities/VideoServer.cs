using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;

namespace webapi
{
    public class VideoServer
    {
        public string uuid { get; set; } = string.Empty;
        public TcpClient? sock_et { get; set; } = null;
        public NetworkStream? networkStream { get; set; } = null;
        public IVideoBusService? listener_videoBus { get; private set; }

       
        public void Dispose()
        {
            networkStream?.Dispose();
            sock_et?.Dispose();
        }

        public void SetVideoBus(IVideoBusService? videoBus)
        {
            if(videoBus == null) throw new ArgumentNullException(nameof(videoBus));
            listener_videoBus = videoBus;
        }

        public async void ReadAndSendJetson(EcodroneBoatMessage v)
        {
            if (v.direction == uuid)
            {
                if (networkStream != null)
                {
                    try
                    {
                        string message_out = JsonConvert.SerializeObject(v);
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
