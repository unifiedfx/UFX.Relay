using System.Net;
using Microsoft.AspNetCore.Connections;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Client;

public class RelayConnectionListenerFactory(IRelayClientOptions clientOptions) : IConnectionListenerFactory
{
    public async ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = new CancellationToken())
    {
        if (endpoint is not WebsocketEndpoint relayEndpoint) throw new NotSupportedException($"{nameof(WebsocketEndpoint)} is required");
        var listener = new RelayConnectionListener(relayEndpoint, clientOptions);
        await listener.Bind();
        return listener;
    }
}