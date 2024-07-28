using UFX.Relay.Tunnel;
using UFX.Relay.Tunnel.Forwarder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelForwarder(options =>
{
    options.DefaultTunnelId = "123";
});
var app = builder.Build();
app.MapTunnelHost();
app.MapTunnelForwarder();
app.MapGet("/", () => builder.Environment.ApplicationName);
app.MapGet("/server", () => "Hello from Server");
app.Run();