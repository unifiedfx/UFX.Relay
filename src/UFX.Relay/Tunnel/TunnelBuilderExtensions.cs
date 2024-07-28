using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel;

public static class TunnelBuilderExtensions
{
    public static IServiceCollection AddTunnelClient(this IServiceCollection services, Action<TunnelClientOptions>? clientOptions = null)
    {
        if (clientOptions != null)
        {
            var options = new TunnelClientOptions();
            clientOptions.Invoke(options);
            services.AddSingleton<TunnelClientOptions>(options);
            services.AddSingleton<ITunnelClientFactory,ClientTunnelClientFactory>();
        }
        services.TryAddSingleton<ITunnelManager, TunnelManager>();
        return services;
    }
    
    public static IEndpointConventionBuilder MapTunnelHost(this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string path = "/tunnel/{tunnelId}",
        Action<WebSocketOptions>? webSocketOptions = null)
    {
        IApplicationBuilder app = endpoints as IApplicationBuilder ?? throw new ArgumentNullException(nameof(endpoints));
        var options = new WebSocketOptions();
        webSocketOptions?.Invoke(options);
        app.UseWebSockets(options);
        return endpoints.MapGet(path, static async (HttpContext context, string tunnelId, ITunnelManager tunnelManager) => {
            if (!context.WebSockets.IsWebSocketRequest) return Results.BadRequest();
            Console.WriteLine($"Tunnel connected: {tunnelId}");
            await tunnelManager.StartTunnelAsync(context, tunnelId);
            return Results.Empty;
        }).ExcludeFromDescription();
    }
}