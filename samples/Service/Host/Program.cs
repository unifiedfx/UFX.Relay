using Host;
using Microsoft.AspNetCore.HttpLogging;
using UFX.Relay.Tunnel;
using UFX.Relay.Tunnel.Forwarder;

foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
    Console.WriteLine($"Environment Variable: {env.Key}:{env.Value}");

var builder = WebApplication.CreateBuilder(args);
var keyVaultUrl = builder.Configuration["KeyVaultCert"];
if(keyVaultUrl != null) builder.WebHost.UseKeyVaultCert(new Uri(keyVaultUrl));
builder.Services.AddTunnelForwarder();
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseHttpLogging();
app.MapTunnelHost();
app.MapTunnelForwarder();
app.MapGet("/host", () => builder.Environment.ApplicationName);
app.Run();
