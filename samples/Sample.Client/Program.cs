
using UFX.Relay.Tunnel;
using UFX.Relay.Tunnel.Listener;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.AddTunnelListener(includeDefaultUrls: true);
builder.Services.AddTunnelClient(options =>
{
    options.TunnelHost = "wss://localhost:7200";
    options.TunnelId = "123";
});
var app = builder.Build();
app.MapGet("/", () => builder.Environment.ApplicationName);
app.MapGet("/client", () => "Hello from Client");
app.Run();