using System.Net.WebSockets;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel;

public sealed class ClientTunnelClientFactory(TunnelClientOptions options) : ITunnelClientFactory
{
    public ValueTask<ClientWebSocket?> CreateAsync()
    {
        if(options.TunnelHost == null || options.TunnelId == null) return new();
        var webSocket = new ClientWebSocket();
        webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
        options.WebSocketOptions?.Invoke(webSocket.Options);
        return new(webSocket);
    }

    public ValueTask<Uri> GetUriAsync()
    {
        if (options.TunnelHost == null) throw new ArgumentNullException(nameof(options.TunnelHost));
        if (options.TunnelId == null) throw new ArgumentNullException(nameof(options.TunnelId));
        var uri = new UriBuilder(options.TunnelHost)
        {
            Path = string.Format(options.TunnelPathTemplate, options.TunnelId)
        };
        return new(uri.Uri);
    }
}

public sealed class HostTunnelClientFactory : ITunnelClientFactory
{
    public async ValueTask<ClientWebSocket?> CreateAsync()
    {
        throw new NotImplementedException();
    }

    public async ValueTask<Uri> GetUriAsync()
    {
        throw new NotImplementedException();
    }
}