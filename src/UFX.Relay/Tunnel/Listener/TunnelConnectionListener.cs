using System.Net;
using Microsoft.AspNetCore.Connections;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelConnectionListener(TunnelEndpoint tunnelEndpoint, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager) : IConnectionListener
{
    private TunnelEndpoint? endpoint;
    
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while(endpoint?.Tunnel == null)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            tunnelEndpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint.TunnelId, cancellationToken);
            await Task.Delay(1000, cancellationToken);
        }
        var channel = await endpoint.Tunnel.GetChannelAsync(endpoint.Tunnel is TunnelHost ? Guid.NewGuid().ToString("N") : null, cancellationToken);
        return new TunnelConnectionContext(channel.QualifiedId.ToString(), channel, endpoint);
        //TODO: Use DefaultConnectionContext instead of TunnelConnectionContext?
        // return new DefaultConnectionContext(channel.QualifiedId.ToString(), channel, null);
    }

    public async Task BindAsync()
    {
        endpoint = tunnelEndpoint;
        endpoint.TunnelId = await tunnelIdProvider.GetTunnelIdAsync() ?? throw new KeyNotFoundException("TunnelId not found");
        var cts = new CancellationTokenSource(5000);
        tunnelEndpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint.TunnelId, cts.Token);
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        endpoint?.Tunnel?.Dispose();
        endpoint = null;
        return ValueTask.CompletedTask;
    }

    public EndPoint EndPoint { get; } = tunnelEndpoint;
    
    public ValueTask DisposeAsync()
    {
        endpoint?.Tunnel?.Dispose();
        return new();
    }
}