using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Forwarder;

public class ForwarderTunnelIdProvider(IHttpContextAccessor accessor, IOptions<TunnelForwarderOptions> options) : ITunnelIdProvider
{
    public async ValueTask<string?> GetTunnelIdAsync() => 
        new (accessor.HttpContext.Request.Headers[options.Value.TunnelIdHeader].FirstOrDefault() ?? options.Value.DefaultTunnelId);
}