using System.Diagnostics;
using System.Net;
using Nerdbank.Streams;
using UFX.Relay.Abstractions;
using Yarp.ReverseProxy.Forwarder;

namespace UFX.Relay.Tunnel.Forwarder;


public class TunnelForwarderHttpClientFactory(ITunnelManager tunnelManager, IHttpContextAccessor accessor, ITunnelIdProvider tunnelIdProvider) : IForwarderHttpClientFactory {

    //TODO: Consider creating a pool of HttpMessageInvoker instances to reuse up to the limit of a MultiplexingStream channel limit
    // effectively there should be a 1-2-1 relationship between the HttpMessageInvoker and the MultiplexingStream channel
    // If/when a HttpMessageInvoker is disposed replace with a new instance from the same channel?
    // The pool will need to be cleared when the MultiplexingStream/relay websocket connection is closed
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        var httpContext = accessor.HttpContext;
        SocketsHttpHandler handler = new SocketsHttpHandler()
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            ActivityHeadersPropagator = (DistributedContextPropagator) new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15.0),
            //Note: may maintain a pool of channelId's here and pass the channelid to GetStreamAsync => RelayConnection.GetChannel
            ConnectCallback = async (ctx, token) =>
            {
                var relayId =  await tunnelIdProvider.GetTunnelIdAsync() ?? throw new KeyNotFoundException();
                var tunnel = await tunnelManager.GetOrCreateTunnelAsync(relayId, token);
                var channel = await tunnel!.GetChannelAsync(tunnel is TunnelHost ? httpContext!.Connection.Id : null, token);
                return channel.AsStream();
            },
        };
        return new HttpMessageInvoker(handler, true);
    }
}
