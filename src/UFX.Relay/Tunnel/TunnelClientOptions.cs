using System.Net.WebSockets;

namespace UFX.Relay.Tunnel;

public sealed class TunnelClientOptions
{
    public string? TunnelId { get; set; }
    public string? TunnelHost { get; set; }
    public string TunnelPathTemplate { get; set; } = "/tunnel/{0}";
    public Action<ClientWebSocketOptions>? WebSocketOptions { get; set; }
}