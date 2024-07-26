using System.Diagnostics.CodeAnalysis;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Server.Extensions;

public static class RelayIEndpointRouteBuilderExtensions {
    public static IEndpointConventionBuilder MapRelay(this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string path = "/relay/{relayId}",
        Action<WebSocketOptions>? webSocketOptions = null)
    {
        var app = endpoints as IApplicationBuilder ?? throw new ArgumentNullException(nameof(endpoints));
        var options = new WebSocketOptions();
        webSocketOptions?.Invoke(options);
        IEndpointConventionBuilder builder = null!;
        app.UseWebSockets(options);
        builder = endpoints.MapGet(path, static async (HttpContext context, string relayId, IRelayConnectionManager relayConnectionManager) => {
            if (!context.WebSockets.IsWebSocketRequest) return Results.BadRequest();
            Console.WriteLine($"Relay connected: {relayId}");
            await relayConnectionManager.AddWebSocket(context, relayId);
            return Results.Empty;
        }).ExcludeFromDescription();
        var pipeline = endpoints.CreateApplicationBuilder()
            .UseMiddleware<RelayForwarderMiddleware>()
            .Build();
        endpoints.Map("{**catch-all}", pipeline);
        return builder;
    }
}