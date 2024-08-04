using System.Net;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelEndpoint : EndPoint
{
    public string? TunnelId { get; set; }
    public Tunnel? Tunnel { get; set; }

    public override string ToString() => (Tunnel?.Uri?.Host ?? base.ToString())!;
}