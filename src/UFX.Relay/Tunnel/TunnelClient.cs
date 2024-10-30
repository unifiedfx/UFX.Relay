using System.Net.WebSockets;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

//TODO: Add collection that tracks multiple ClientWebSocket connections
// Pass a ClientWebSocket Func to the constructor that creates a new ClientWebSocket instance
// Automatically create a mew ClientWebSocket when a channel limit is reached
// Would need a low/high watermark for the creation/closing of ClientWebSocket instances
// What would be the best way to select an existing ClientWebSocket instance to use?
public class TunnelClient(ClientWebSocket webSocket, MultiplexingStream stream) : Tunnel(stream)
{

    protected override async ValueTask DisposeAsyncCore()
    {
        try
        {
            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, default);
        }
        catch (OperationCanceledException) { }
        await base.DisposeAsyncCore();
    }
}