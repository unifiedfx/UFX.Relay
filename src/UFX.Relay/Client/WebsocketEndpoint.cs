using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Client;

public class WebsocketEndpoint(Uri uri, Action<ClientWebSocketOptions>?  options = default) : UriEndPoint(uri)
{
    public ClientWebSocket CreateClient(IRelayClientOptions relayOptions)
    {
        var clientWebSocket = new ClientWebSocket();
        relayOptions.WebSocketOptions?.Invoke(clientWebSocket.Options);
        options?.Invoke(clientWebSocket.Options);
        return clientWebSocket;
    }

    public override string ToString() => $"{Uri.Host}:{Uri.Port}";
}