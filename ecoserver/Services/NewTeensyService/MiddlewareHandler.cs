using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace webapi.Services.NewTeensyService
{
    public class MiddlewareHandler : IMiddlewareHandler
    {
        private readonly ILogger<MiddlewareHandler> _logger;
        private readonly ISocketTeensyService _socketTeensy;

        public event EventHandler<NewClientEventArgs> Handler;
        public event EventHandler<BusEventMessage> HandlerCommand;

        private WebSocket? _webSocket = null;


        public MiddlewareHandler(ILogger<MiddlewareHandler> log, ISocketTeensyService socketTeensy)
        {
            _logger = log;
            _socketTeensy = socketTeensy;
            Handler += _socketTeensy.NewClientConnectionEvent;
            HandlerCommand += _socketTeensy.GotCommand;
        }


        public async Task HandlingWs(WebSocket webSocket, string userid = "userprimo") //teensyid = "cazzoduro"
        {
            _webSocket = webSocket;

            NewClientEventArgs args = new NewClientEventArgs();
            args.userid = userid;
            args.maskedTeensyId = "cazzoduro";
           
            Handler(this, args);

            while (_webSocket.State == WebSocketState.Open)
            {
                //_logger.LogInformation("still running ID: " + teensyid);

                var buffer = new byte[1024 * 4];
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (receiveResult.Count > 0) //is greater than a single byte P
                {

                    if (buffer[0] != 0x50 && buffer[0] != 0x53)
                    {
                        BusEventMessage commandEvent = new BusEventMessage(args.maskedTeensyId, buffer.Take(receiveResult.Count).ToArray());
                        await Task.Delay(200);
                        HandlerCommand(this, commandEvent);
                    }

                    ChannelTeensyMessage? messageTeensy = _socketTeensy.ReadOnChannel(args.maskedTeensyId);

                    if (messageTeensy != null)
                    {
                        if (messageTeensy.data_in != null)
                        {
                            await _webSocket.SendAsync(messageTeensy.data_in, WebSocketMessageType.Binary, true, CancellationToken.None);
                            messageTeensy.data_in = null;
                        }


                    }
                    else
                    {
                        byte[] pong = new byte[2];
                        pong = [0x50, 0x50];
                        await _webSocket.SendAsync(pong, WebSocketMessageType.Binary, true, CancellationToken.None);
                    }

                    await Task.Delay(100);
                }
   
            }
            
            _logger.LogInformation(/*$"{userid} */"process is ending");
        }
    }

    
}
