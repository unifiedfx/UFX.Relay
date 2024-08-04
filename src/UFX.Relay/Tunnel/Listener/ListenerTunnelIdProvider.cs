using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class ListenerTunnelIdProvider(TunnelListenerOptions listenerOptions, TunnelClientOptions? clientOptions) : ITunnelIdProvider
{
    public ValueTask<string?> GetTunnelIdAsync()
    {
        return new ValueTask<string?>(
            listenerOptions.DefaultTunnelId 
            ?? clientOptions?.TunnelId 
            ?? (clientOptions?.TunnelHost != null ? new Uri(clientOptions.TunnelHost).GetTunnelIdFromHost() : null));
    }
}