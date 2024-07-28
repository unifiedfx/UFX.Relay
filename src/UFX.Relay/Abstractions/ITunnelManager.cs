namespace UFX.Relay.Abstractions;

public interface ITunnelManager
{
    Task<Tunnel.Tunnel?> GetOrCreateTunnelAsync(string tunnelId, CancellationToken cancellationToken = default);
    Task StartTunnelAsync(HttpContext context, string tunnelId, CancellationToken cancellationToken = default);
}