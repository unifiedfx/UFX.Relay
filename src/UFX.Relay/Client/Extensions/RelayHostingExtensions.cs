using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Client.Extensions;

public static class RelayHostingExtensions
{
    public static KestrelServerOptions ListenOnRelay(this KestrelServerOptions options, string uri,
        Action<ClientWebSocketOptions>? webSocketOptions = null) => options.ListenOnRelay(new Uri(uri), webSocketOptions);

    public static KestrelServerOptions ListenOnRelay(this KestrelServerOptions options, Uri uri, Action<ClientWebSocketOptions>? webSocketOptions = null) {
        ArgumentNullException.ThrowIfNull(options);
        options.Listen(new WebsocketEndpoint(uri, webSocketOptions));
        Console.WriteLine("Added relay listener: {0}", uri);
        return options;
    }

    public static IWebHostBuilder AddRelayListener(this IWebHostBuilder builder, string uri,
        Action<RelayClientOptions>? configureOptions = null, bool includeDefaultUrls = false) =>
        builder.AddRelayListener(new Uri(uri), configureOptions, includeDefaultUrls);

    public static IWebHostBuilder AddRelayListener(this IWebHostBuilder builder, Uri uri,
        Action<RelayClientOptions>? configureOptions = null, bool includeDefaultUrls = false)
    {
        builder.ConfigureKestrel(options =>
        {
            var urls = builder.GetSetting(WebHostDefaults.ServerUrlsKey)?.Split(';', StringSplitOptions.TrimEntries);
            if (includeDefaultUrls && urls is {Length: > 0}) options.ListenOnUrls(urls);
            options.ListenOnRelay(uri);
        });
        builder.ConfigureServices(services =>
        {
            if (configureOptions != null)  services.Configure(configureOptions);
            services.AddRelay();
        });
        return builder;
    }

    public static IServiceCollection AddRelay(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IRelayClientOptions>(provider => provider.GetService<IOptions<RelayClientOptions>>()?.Value
                                                               ?? new RelayClientOptions());
        services.TryAddSingleton<SocketTransportFactory>();
        services.TryAddSingleton(provider => 
            new RelayConnectionListenerFactory(provider.GetRequiredService<IRelayClientOptions>()));
        services.AddSingleton<IConnectionListenerFactory, RelayCompositeTransportFactory>();
        return services;
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