using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRelayHost(this IServiceCollection builder, Action<RelayServerOptions>? configureOptions = null) {
        ArgumentNullException.ThrowIfNull(builder);
        if (configureOptions != null) builder.Configure(configureOptions);
        builder
            .AddHttpForwarder()
            .AddHttpContextAccessor()
            .AddSingleton<IRelayServerOptions>(provider => 
                provider.GetService<IOptions<RelayServerOptions>>()?.Value ?? new RelayServerOptions())
            .AddSingleton<RelayForwarderMiddleware>()
            .AddSingleton<RelayHttpClientFactory>()
            .AddSingleton<IRelayConnectionManager, DefaultRelayConnectionManager>()
            .AddSingleton<IRelayIdProvider, DefaultRelayIdProvider>();
        return builder;
    }    
}