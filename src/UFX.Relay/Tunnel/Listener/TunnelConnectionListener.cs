using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public sealed class TunnelConnectionListener(TunnelEndpoint endpoint, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager, IOptions<TunnelListenerOptions> options) : IConnectionListener
{
    private readonly SemaphoreSlim getTunnelSemaphore = new(1, 1);
    private CancellationTokenSource unbindTokenSource = new();
    public EndPoint EndPoint => endpoint;

    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        var linkedToken = CancellationTokenSource
            .CreateLinkedTokenSource(unbindTokenSource.Token, cancellationToken)
            .Token;
        
        while (! linkedToken.IsCancellationRequested)
        {
            await GetTunnelAsync(linkedToken);
            if (endpoint.Tunnel == null) return null;
            try
            {
                var channel = await endpoint.Tunnel
                    .GetChannelAsync(endpoint.Tunnel is TunnelHost ? Guid.NewGuid().ToString("N") : null, linkedToken);
                return new TunnelConnectionContext(channel.QualifiedId.ToString(), channel, endpoint);
            }
            catch (UnderlyingStreamClosedException)
            {
                // The WebSocket stream closed underneath us while waiting for the channel:
                continue;
            }
        }

        // Fall through to here if caller cancelled, or Listener was ‘unbound’:
        return null;
    }
    
    public async Task BindAsync()
    {
        unbindTokenSource = new CancellationTokenSource();
        endpoint.TunnelId = await tunnelIdProvider.GetTunnelIdAsync() ?? throw new KeyNotFoundException("TunnelId not found");
        ReconnectTunnelAsync(unbindTokenSource.Token);
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        await unbindTokenSource.CancelAsync();
        if(endpoint.Tunnel is not null) await endpoint.Tunnel.DisposeAsync();
    }
    private async ValueTask GetTunnelAsync(CancellationToken cancellationToken = default)
    {
        if (endpoint.Tunnel is {Completion.IsCompleted: false}) return;
        var linkedToken = CancellationTokenSource
            .CreateLinkedTokenSource(unbindTokenSource.Token, cancellationToken).Token;
        await getTunnelSemaphore.WaitAsync(linkedToken);
        if (endpoint.Tunnel is not {Completion.IsCompleted: false} && !linkedToken.IsCancellationRequested)
        {
            while ((endpoint.Tunnel == null || endpoint.Tunnel.Completion.IsCompleted) &&
                   !linkedToken.IsCancellationRequested)
            {
                if (linkedToken.IsCancellationRequested) return;
                endpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint!.TunnelId!, linkedToken);
            }

        } 
        getTunnelSemaphore.Release();
    }
    private async Task ReconnectTunnelAsync(CancellationToken cancellationToken = default)
    {
        var timer = new PeriodicTimer(options.Value.ReconnectInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken)) await GetTunnelAsync(cancellationToken);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (endpoint.Tunnel == null) return;
        await endpoint.Tunnel.DisposeAsync();
    }
}