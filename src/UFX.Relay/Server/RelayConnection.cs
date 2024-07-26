using System.Net.WebSockets;
using Nerdbank.Streams;

namespace UFX.Relay.Server;


public class RelayConnection(WebSocket webSocket, MultiplexingStream stream) : IDisposable
{
    //TODO: Change to use seeded channels and save on the per request exchange
    //Seeded channels will need a connection limit per relay/tunnel and this class will need to track connection ids
    public async Task<MultiplexingStream.Channel> GetChannel(string channelId)
    {
        var channel = await stream.OfferChannelAsync(channelId, CancellationToken.None);
        return channel;
    }
    
    public void Dispose() {
        webSocket.Dispose();
        stream.Dispose();
    }
}