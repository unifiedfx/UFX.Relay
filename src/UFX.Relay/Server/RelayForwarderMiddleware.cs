using UFX.Relay.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms.Builder;

namespace UFX.Relay.Server;

public class RelayForwarderMiddleware(IHttpForwarder forwarder, RelayHttpClientFactory clientFactory, ITransformBuilder builder, IRelayServerOptions options, IRelayIdProvider relayIdProvider, IRelayConnectionManager connectionManager) : IMiddleware
{
    private HttpTransformer? transformer;
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var relayId = await relayIdProvider.GetRelayIdAsync(context);
        if (relayId == null || !await connectionManager.CanForward(context))
        {
            await next(context);
            return;
        }
        var client = clientFactory.CreateClient(new ForwarderHttpClientContext {NewConfig = HttpClientConfig.Empty});
        //Note: The destination prefix needs to be http so that SSL in not used over the multiplex channel, instead the websocket should use SSL/WSS
        var destinationPrefix = $"http://{context.Request.Host}";
        if (options.Transformer != null) transformer ??= builder.Create(options.Transformer);
        _ = await forwarder.SendAsync(context, destinationPrefix, client, ForwarderRequestConfig.Empty, transformer ?? HttpTransformer.Default);
    }
}