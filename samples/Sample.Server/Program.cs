using UFX.Relay.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelayHost(options =>
{
    options.DefaultRelayId = "123";
});
var app = builder.Build();
app.MapRelay().RequireAuthorization();
app.MapGet("/", () => builder.Environment.ApplicationName);
app.MapGet("/server", () => "Hello from Server");
app.Run();