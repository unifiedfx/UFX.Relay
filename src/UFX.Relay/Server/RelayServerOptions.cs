using UFX.Relay.Abstractions;
using Yarp.ReverseProxy.Transforms.Builder;

namespace UFX.Relay.Server;

public class RelayServerOptions : IRelayServerOptions {
    public string RelayIdHeader { get; set; } = "RelayId";
    public string? DefaultRelayId { get; set; }
    public Action<TransformBuilderContext>? Transformer { get; set; }
}