namespace UFX.Relay.Tunnel.Listener;

public class TunnelListenerOptions
{
    public string? DefaultTunnelId { get; set; }
    public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(10);
}