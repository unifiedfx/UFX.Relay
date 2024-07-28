using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace UFX.Relay.Tunnel.Listener;

public class TunnelCompositeTransportFactory(TunnelConnectionListenerFactory tunnelConnectionListenerFactory, SocketTransportFactory socketTransportFactory) : IConnectionListenerFactory
{
    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default) =>
        endpoint is TunnelEndpoint
            ? tunnelConnectionListenerFactory.BindAsync(endpoint, cancellationToken)
            : socketTransportFactory.BindAsync(endpoint, cancellationToken);
}