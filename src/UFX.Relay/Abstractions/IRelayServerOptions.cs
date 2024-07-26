using Yarp.ReverseProxy.Transforms.Builder;

namespace UFX.Relay.Abstractions;

public interface IRelayServerOptions
{
    string RelayIdHeader { get; set; }
    string? DefaultRelayId { get; set; }
    Action<TransformBuilderContext>? Transformer { get; set; }
}