using Microsoft.Extensions.Options;
using UFX.Relay.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms.Builder;

namespace UFX.Relay.Tunnel.Forwarder;

public class TunnelForwarderMiddleware(IHttpForwarder forwarder, TunnelForwarderHttpClientFactory clientFactory, ITransformBuilder builder, IOptions<TunnelForwarderOptions> options, ITunnelIdProvider tunnelIdProvider, ITunnelManager tunnelManager) : IMiddleware
{
    private HttpTransformer? transformer;
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tunnelId = await tunnelIdProvider.GetTunnelIdAsync();
        if (tunnelId == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        var tunnel = await tunnelManager.GetOrCreateTunnelAsync(tunnelId, context.RequestAborted);
        if (tunnel == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        var client = clientFactory.CreateClient(new ForwarderHttpClientContext {NewConfig = HttpClientConfig.Empty});
        //Note: The destination prefix needs to be http so that SSL in not used over the multiplex channel, instead the websocket should use SSL/WSS
        var destinationPrefix = $"http://{context.Request.Host}";
        if (options.Value.Transformer != null) transformer ??= builder.Create(options.Value.Transformer);
        _ = await forwarder.SendAsync(context, destinationPrefix, client, ForwarderRequestConfig.Empty, transformer ?? HttpTransformer.Default);
    }
}