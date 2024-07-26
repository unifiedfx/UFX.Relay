using System.Collections.Concurrent;
using Nerdbank.Streams;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Server;

public class DefaultRelayConnectionManager(IRelayIdProvider relayIdProvider) : IRelayConnectionManager {
    private readonly ConcurrentDictionary<string, RelayConnection> connections = new();

    public async Task AddWebSocket(HttpContext context, string relayId) {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await using var stream = await MultiplexingStream.CreateAsync(webSocket.AsStream(), new MultiplexingStream.Options {
            ProtocolMajorVersion = 3
        });
        var connection = new RelayConnection(webSocket, stream);
        connections.AddOrUpdate(relayId, _ => connection, (_, oldConnection) => {
            oldConnection.Dispose();
            return connection;
        });
        try
        { 
            await stream.Completion;
        }
        catch (Exception e)
        {
            Console.WriteLine("Relay: {0}, Error: {1}", relayId, e.Message);
        }
        finally
        {
            connections.TryRemove(new KeyValuePair<string, RelayConnection>(relayId, connection));
        }
    }

    public async Task<Stream> GetStreamAsync(HttpContext context) {
        var relayId =  await relayIdProvider.GetRelayIdAsync(context) ?? throw new KeyNotFoundException();
        if (!connections.TryGetValue(relayId, out var connection)) throw new KeyNotFoundException();
        var channel = await connection.GetChannel(context.Connection.Id);
        return channel.AsStream(true);
    }

    public async ValueTask<bool> CanForward(HttpContext context)
    {
        var relayId = await relayIdProvider.GetRelayIdAsync(context);
        return relayId != null && connections.ContainsKey(relayId);
    }
}