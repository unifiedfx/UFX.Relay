using System.Net.WebSockets;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

public class TunnelHost(WebSocket webSocket, MultiplexingStream stream) : Tunnel(stream)
{
    public override void Dispose() {
        webSocket.Dispose();
        base.Dispose();
    }    
}