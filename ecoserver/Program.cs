

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://localhost:5001"); 


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

builder.Services.AddControllers();



builder.Services.AddSingleton<IActiveBoatTracker, ActiveBoatTracker>();


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



app.UseWebSockets(webSocketOptions);


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
