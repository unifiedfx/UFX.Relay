using Yarp.ReverseProxy.Transforms.Builder;

namespace UFX.Relay.Tunnel.Forwarder;

public class TunnelForwarderOptions
{
    public string? DefaultTunnelId { get; set; }
    public string TunnelIdHeader { get; set; } = "TunnelId";
    public Action<TransformBuilderContext>? Transformer { get; set; }
}