using System.Diagnostics;
using System.Net.WebSockets;
using webapi;
using webapi.Services.NewTeensyService;




public class WebSocketCustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketCustomMiddleware> _logger;
    private readonly IServiceProvider _sc;

    
    public WebSocketCustomMiddleware(
        RequestDelegate next, 
        ILogger<WebSocketCustomMiddleware> logger,IServiceProvider serviceProvide)
    {
        _next = next;
        //_eventBus = eventBus;
        _logger = logger;
        _sc = serviceProvide;
    }


    public async Task InvokeAsync(HttpContext context)
    {
    
        if (context.WebSockets.IsWebSocketRequest)
        {
            using (WebSocket WS = await context.WebSockets.AcceptWebSocketAsync())
            {
                //to check if client is connected check database or from token stored in memory
                string idsocket = "userprimo";///Guid.NewGuid().ToString();


                using (IServiceScope scope = _sc.CreateScope())
                {
                    IMiddlewareHandler middlewareProcessHandler =
                            scope.ServiceProvider.GetRequiredService<IMiddlewareHandler>();

                    await middlewareProcessHandler.HandlingWs(WS, "cazzoduro");
                }

                /*if (context.Request.Path == "/ws")
                {
                     // = "ecodroneTestUser";  Guid.NewGuid().ToString();

                    using (IServiceScope scope = _sc.CreateScope())
                    {
                        IMiddlewareHandler middlewareProcessHandler =
                                scope.ServiceProvider.GetRequiredService<IMiddlewareHandler>();

                        await middlewareProcessHandler.HandlingWs(WS, "cazzoduro");
                    }
                }else
                {
                    using (IServiceScope scope = _sc.CreateScope())
                    {
                        IVideoServiceHandler middleWareHandlerVideoSocket =
                                scope.ServiceProvider.GetRequiredService<IVideoServiceHandler>();

                        await middleWareHandlerVideoSocket.HandlingWsVideo(WS, "cazzoduro");
                    }

                    _logger.LogInformation("connected of {0}", context.Request.Path);
                }*/


                //await Task.Delay(1000);
            }
        }
        else
        {
            await _next(context);
        }
    }

}

public static class WebSocketCustomMiddlewareRequest
{
    public static IApplicationBuilder UseWebSocketCustomMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<WebSocketCustomMiddleware>();
    }
}

