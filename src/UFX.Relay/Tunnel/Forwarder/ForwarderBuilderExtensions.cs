using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Forwarder;

public static class ForwarderBuilderExtensions
{
    public static IEndpointConventionBuilder MapTunnelForwarder(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string path = "{**catch-all}")
    {
        var pipeline = endpoints.CreateApplicationBuilder()
            .UseMiddleware<TunnelForwarderMiddleware>()
            .Build();
        return endpoints.Map(path, pipeline);
    }

    public static IServiceCollection AddTunnelForwarder(this IServiceCollection services, Action<TunnelForwarderOptions>? forwarderOptions)
    {
        if(forwarderOptions != null) services.Configure(forwarderOptions);
        services.AddHttpForwarder();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<ITunnelClientFactory, HostTunnelClientFactory>();
        services.AddSingleton<ITunnelIdProvider, ForwarderTunnelIdProvider>();
        services.AddSingleton<TunnelForwarderHttpClientFactory>();
        services.AddSingleton<TunnelForwarderMiddleware>();
        services.TryAddSingleton<ITunnelManager, TunnelManager>();
        return services;
    }
}