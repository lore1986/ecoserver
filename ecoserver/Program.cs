

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://localhost:5001"); // ---->

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();
builder.Services.AddControllers();



//builder.Services.AddSingleton<VideoSocketSingleton>();
// builder.Services.AddSingleton<ISocketTeensyService, SocketTeensyService>();
// builder.Services.AddScoped<IMiddlewareHandler,MiddlewareHandler> ();
// builder.Services.AddSingleton<IVideoSocketSingleton, VideoSocketSingleton>();

var app = builder.Build();


// app.UseForwardedHeaders(new ForwardedHeadersOptions
// {
//     ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto // ---->
// });

app.UseSwagger();
app.UseSwaggerUI();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(1),
};


// app.UseForwardedHeaders(new ForwardedHeadersOptions
// {
//     ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
// });



app.UseWebSockets(webSocketOptions);
//app.UseWebSocketCustomMiddleware();

app.UseRouting();
app.MapControllers();

app.UseHttpsRedirection(); // ---->
app.UseStaticFiles();

app.UseCors((options) =>
{
    options.AllowAnyMethod();
    options.AllowAnyHeader();
});

await app.RunAsync();
