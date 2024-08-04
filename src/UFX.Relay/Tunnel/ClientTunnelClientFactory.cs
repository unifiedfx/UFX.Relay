using System.Net.WebSockets;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel;

public sealed class ClientTunnelClientFactory(TunnelClientOptions options, ITunnelIdProvider tunnelIdProvider) : ITunnelClientFactory
{
    public async ValueTask<ClientWebSocket?> CreateAsync()
    {
        var tunnelId = await tunnelIdProvider.GetTunnelIdAsync();
        if (tunnelId == null) return null;
        var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        options.WebSocketOptions?.Invoke(webSocket.Options);
        return webSocket;
    }

    public async ValueTask<Uri> GetUriAsync()
    {
        if (options.TunnelHost == null) throw new ArgumentNullException(nameof(options.TunnelHost));
        var tunnelId = await tunnelIdProvider.GetTunnelIdAsync();
        var uri = new UriBuilder(options.TunnelHost)
        {
            Path = string.Format(options.TunnelPathTemplate, tunnelId)
        };
        return uri.Uri;
    }
}