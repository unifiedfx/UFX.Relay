using UFX.Relay.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.AddRelayListener("wss://localhost:7200", options =>
{
    options.GetRelayId = _ => "123";
}, includeDefaultUrls: true);

var app = builder.Build();
app.MapGet("/", () => builder.Environment.ApplicationName);
app.MapGet("/client", () => "Hello from Client");
app.Run();