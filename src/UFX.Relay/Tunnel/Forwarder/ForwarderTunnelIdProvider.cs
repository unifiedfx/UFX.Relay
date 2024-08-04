using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Forwarder;

public class ForwarderTunnelIdProvider(IHttpContextAccessor accessor, IOptions<TunnelForwarderOptions> options)
    : ITunnelIdProvider
{
    public ValueTask<string?> GetTunnelIdAsync()
    {
        return new(accessor.HttpContext == null ? null : GetFromQuery() ?? accessor.HttpContext.GetTunnelIdFromHost() ?? GetFromHeader());
        string? GetFromQuery() => accessor.HttpContext.Request.Query[options.Value.TunnelIdHeader].FirstOrDefault();
        string? GetFromHeader() =>
            accessor.HttpContext.Request.Headers[options.Value.TunnelIdHeader].FirstOrDefault() ??
            options.Value.DefaultTunnelId;
    }
}