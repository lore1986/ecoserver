using System.Net.WebSockets;

namespace webapi.Services.NewTeensyService
{
    public interface IMiddlewareHandler
    {
        Task HandlingWs(WebSocket webSocket, string userid);
    }
}