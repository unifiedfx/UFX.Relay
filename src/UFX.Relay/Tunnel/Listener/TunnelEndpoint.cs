using System.Net;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelEndpoint : EndPoint
{
    public string? TunnelId { get; set; }
    public Tunnel? Tunnel { get; set; }
    // Note: Hacky way to return the tunnel:// prefix to show up in the 'Now listening on' message as it's prefixed with http://
    public override string ToString() => ("\x1b[7Dtunnel://" + (Tunnel?.Uri?.Host ?? $"{TunnelId}")).PadRight(12);
}