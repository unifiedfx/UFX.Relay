using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel.Listener;

public static class ListenerBuilderExtensions
{
    private static bool tunnelListenerAdded;
    public static IWebHostBuilder AddTunnelListener(this IWebHostBuilder builder, Action<TunnelListenerOptions>? tunnelOptions = null, bool includeDefaultUrls = false)
    {
        if (tunnelListenerAdded)
        {
            Console.WriteLine("Tunnel Listener already added");
            return builder;
        }
        tunnelListenerAdded = true;
        builder.ConfigureKestrel(options =>
        {
            options.ListenOnTunnel();
            var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey)?.Split(';', StringSplitOptions.TrimEntries);
            if (includeDefaultUrls && urls is {Length: > 0}) options.ListenOnUrls(urls);
        });
        builder.ConfigureServices(services =>
        {
            services.AddTunnelListener(tunnelOptions);
        });
        return builder;
    }
    
    private static IServiceCollection AddTunnelListener(this IServiceCollection services, Action<TunnelListenerOptions>? tunnelOptions = null)
    {
        if(tunnelOptions != null) services.Configure(tunnelOptions);
        services.TryAddSingleton<ITunnelIdProvider>(provider => 
            new ListenerTunnelIdProvider(provider.GetRequiredService<IOptions<TunnelListenerOptions>>().Value,provider.GetService<TunnelClientOptions>()));
        services.TryAddSingleton<ITunnelManager, TunnelManager>();
        services.TryAddSingleton<SocketTransportFactory>();
        services.TryAddSingleton(provider => 
            new TunnelConnectionListenerFactory(provider.GetRequiredService<ITunnelIdProvider>(),provider.GetRequiredService<ITunnelManager>()));
        services.AddSingleton<IConnectionListenerFactory, TunnelCompositeTransportFactory>();
        return services;        
    }
    
    private static KestrelServerOptions ListenOnTunnel(this KestrelServerOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        options.Listen(new TunnelEndpoint());
        Console.WriteLine("Added tunnel listener");
        return options;
    }
    
    private static KestrelServerOptions ListenOnUrls(this KestrelServerOptions options, params string[] urls)
    {
        foreach (var url in urls)
        {
            var uri = new Uri(url);
            if (IPEndPoint.TryParse(uri.Host, out var endpoint))
            {
                options.Listen(endpoint, HandleOptions(uri));
                Console.WriteLine("Added listener: {0}", uri);
                continue;
            }
            if (uri.Host != "localhost") continue;
            options.ListenAnyIP(uri.Port, HandleOptions(uri));
            Console.WriteLine("Added listener: {0}", uri);
        }
        return options;
        Action<ListenOptions> HandleOptions(Uri uri)
        {
            return listenOptions =>
            {
                try
                {
                    if(uri.Scheme == Uri.UriSchemeHttps) listenOptions.UseHttps();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            };
        }
    }    
}