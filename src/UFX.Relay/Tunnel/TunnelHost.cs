using System.Net.WebSockets;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

//TODO: Add collection that tracks multiple WebSocket connections
// Expose a AddWebSocket method that adds a WebSocket to the collection
public class TunnelHost(WebSocket webSocket, MultiplexingStream stream) : Tunnel(stream)
{
    protected override async ValueTask DisposeAsyncCore()
    {
        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
        await base.DisposeAsyncCore();
    }
}