using System.Net;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelEndpoint : EndPoint
{
    // public Uri? Uri { get; set; }
    public string? TunnelId { get; set; }
    // public override string ToString() => $"{Uri.Host}:{Uri.Port}";
}