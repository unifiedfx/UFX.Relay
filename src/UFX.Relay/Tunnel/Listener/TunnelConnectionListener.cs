using System.Net;
using Microsoft.AspNetCore.Connections;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelConnectionListener(TunnelEndpoint tunnelEndpoint, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager) : IConnectionListener
{
    private TunnelEndpoint? endpoint;
    private CancellationTokenSource unbindTokenSource = new();
   
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        var tcs = CancellationTokenSource.CreateLinkedTokenSource(unbindTokenSource.Token, cancellationToken);
        var token = tcs.Token;
        while(endpoint?.Tunnel == null)
        {
            if (token.IsCancellationRequested) return null;
            tunnelEndpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint!.TunnelId!, token);
            await Task.Delay(1000, token);
        }
        try
        {
            var channel = await endpoint.Tunnel
                .GetChannelAsync(endpoint.Tunnel is TunnelHost ? Guid.NewGuid().ToString("N") : null, token)
                .ConfigureAwait(false);
            return new TunnelConnectionContext(channel.QualifiedId.ToString(), channel, endpoint);
        }
        catch (OperationCanceledException) { }
        return null;
    }

    public async Task BindAsync()
    {
        unbindTokenSource = new CancellationTokenSource();
        endpoint = tunnelEndpoint;
        endpoint.TunnelId = await tunnelIdProvider.GetTunnelIdAsync() ?? throw new KeyNotFoundException("TunnelId not found");
        var cts = new CancellationTokenSource(10000);
        tunnelEndpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint.TunnelId, cts.Token);
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        await unbindTokenSource.CancelAsync();
        if(endpoint?.Tunnel is not null) await endpoint.Tunnel.DisposeAsync();
        endpoint = null;
    }

    public EndPoint EndPoint { get; } = tunnelEndpoint;
    
    public ValueTask DisposeAsync()
    {
        endpoint?.Tunnel?.Dispose();
        return new();
    }
}