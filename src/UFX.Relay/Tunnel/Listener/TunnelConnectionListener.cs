using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelConnectionListener(TunnelEndpoint tunnelEndpoint, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager, IOptions<TunnelListenerOptions> options) : IConnectionListener
{
    private TunnelEndpoint? endpoint;
    private CancellationTokenSource unbindTokenSource = new();
    public EndPoint EndPoint { get; } = tunnelEndpoint;
   
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        if(endpoint?.Tunnel == null) return null;
        try
        {
            var channel = await endpoint.Tunnel
                .GetChannelAsync(endpoint.Tunnel is TunnelHost ? Guid.NewGuid().ToString("N") : null, cancellationToken)
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
        ReconnectTunnelAsync(unbindTokenSource.Token);
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        await unbindTokenSource.CancelAsync();
        if(endpoint?.Tunnel is not null) await endpoint.Tunnel.DisposeAsync();
        endpoint = null;
    }
    private async ValueTask GetTunnelAsync(CancellationToken cancellationToken = default)
    {
        if(endpoint?.Tunnel is {Completion.IsCompleted: false}) return;
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(unbindTokenSource.Token, cancellationToken).Token;
        while((endpoint?.Tunnel == null || endpoint.Tunnel.Completion.IsCompleted) && !linkedToken.IsCancellationRequested)
        {
            if (linkedToken.IsCancellationRequested || endpoint == null) return;
            endpoint.Tunnel = await tunnelManager.GetOrCreateTunnelAsync(endpoint!.TunnelId!, linkedToken).ConfigureAwait(false);
        }
    }
    private async Task ReconnectTunnelAsync(CancellationToken cancellationToken = default)
    {
        await GetTunnelAsync(cancellationToken);
        var timer = new PeriodicTimer(options.Value.ReconnectInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken)) await GetTunnelAsync(cancellationToken);
    }
    
    public async ValueTask DisposeAsync()
    {
        if (endpoint?.Tunnel == null) return;
        await endpoint.Tunnel.DisposeAsync();
    }
}