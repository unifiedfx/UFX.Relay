using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace UFX.Relay.Client;

public class RelayCompositeTransportFactory(RelayConnectionListenerFactory relayConnectionListenerFactory, SocketTransportFactory socketTransportFactory) : IConnectionListenerFactory
{
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = new CancellationToken()) =>
        endpoint is WebsocketEndpoint
            ? relayConnectionListenerFactory.BindAsync(endpoint, cancellationToken)
            : socketTransportFactory.BindAsync(endpoint, cancellationToken);
}