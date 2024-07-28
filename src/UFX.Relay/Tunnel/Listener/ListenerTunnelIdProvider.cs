using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public class ListenerTunnelIdProvider(TunnelListenerOptions listenerOptions, TunnelClientOptions? clientOptions) : ITunnelIdProvider
{
    public ValueTask<string?> GetTunnelIdAsync() => new(listenerOptions.DefaultTunnelId ?? clientOptions?.TunnelId);
}