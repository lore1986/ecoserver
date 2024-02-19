using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using webapi;
using webapi.Services.DatabaseService;
using webapi.Services;
using webapi.Services.NewTeensyService;
using webapi.Services.BusService;
using webapi.Services.SocketService;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Cryptography.X509Certificates;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddControllers();
builder.Services.AddSingleton<VideoSocketSingleton>();


builder.Services.AddSingleton<ISocketTeensyService, SocketTeensyService>();
builder.Services.AddScoped<IMiddlewareHandler,MiddlewareHandler> ();
builder.Services.AddSingleton<IVideoSocketSingleton, VideoSocketSingleton>();

var app = builder.Build();


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
};


// app.UseForwardedHeaders(new ForwardedHeadersOptions
// {
//     ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
// });

Debug.WriteLine("im just before websockets");


app.UseWebSockets(webSocketOptions);
app.UseWebSocketCustomMiddleware();

app.UseRouting();
app.MapControllers();

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors((options) =>
{
    options.AllowAnyMethod();
    options.AllowAnyHeader();
});

await app.RunAsync();
