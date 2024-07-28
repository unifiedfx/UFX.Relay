using System.Net;
using Microsoft.AspNetCore.Connections;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelConnectionListener(TunnelEndpoint tunnelEndpoint, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager) : IConnectionListener
{
    private Tunnel? tunnel;
    
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while(tunnel == null)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            tunnel = await tunnelManager.GetOrCreateTunnelAsync(tunnelEndpoint.TunnelId, CancellationToken.None);
            await Task.Delay(1000, cancellationToken);
        }
        var channel = await tunnel.GetChannelAsync(tunnel is TunnelHost ? Guid.NewGuid().ToString("N") : null, cancellationToken);
        return new TunnelConnectionContext(channel.QualifiedId.ToString(), channel, tunnelEndpoint);
        //TODO: Use DefaultConnectionContext instead of TunnelConnectionContext?
        // return new DefaultConnectionContext(channel.QualifiedId.ToString(), channel, null);
    }

    public async Task BindAsync()
    {
        tunnelEndpoint.TunnelId = await tunnelIdProvider.GetTunnelIdAsync() ?? throw new KeyNotFoundException("TunnelId not found");
        //TODO: This needs to return without waiting for the websocket to connect
        // otherwise the server will block all listeners until the client connects
        tunnel = await tunnelManager.GetOrCreateTunnelAsync(tunnelEndpoint.TunnelId, CancellationToken.None);
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        tunnel?.Dispose();
        tunnel = null;
    }

    public EndPoint EndPoint { get; } = tunnelEndpoint;
    
    public ValueTask DisposeAsync()
    {
        tunnel?.Dispose();
        return new();
    }
}