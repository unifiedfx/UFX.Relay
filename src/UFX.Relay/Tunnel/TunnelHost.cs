using System.Net.WebSockets;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

//TODO: Add collection that tracks multiple WebSocket connections
// Expose a AddWebSocket method that adds a WebSocket to the collection
public class TunnelHost(WebSocket webSocket, MultiplexingStream stream) : Tunnel(stream)
{
    public override void Dispose() {
        webSocket.Dispose();
        base.Dispose();
    }    
}