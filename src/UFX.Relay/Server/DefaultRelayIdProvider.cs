using UFX.Relay.Abstractions;

namespace UFX.Relay.Server;

public class DefaultRelayIdProvider(IRelayServerOptions options) : IRelayIdProvider {
    public ValueTask<string?> GetRelayIdAsync(HttpContext context) => 
        new (context.Request.Headers[options.RelayIdHeader].FirstOrDefault() ?? options.DefaultRelayId);
    
}