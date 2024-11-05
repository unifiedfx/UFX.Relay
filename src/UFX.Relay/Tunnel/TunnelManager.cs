using System.Collections.Concurrent;
using System.Net.WebSockets;
using Nerdbank.Streams;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Tunnel;

public class TunnelManager(ILogger<TunnelManager> logger, IEnumerable<ITunnelClientFactory> tunnelClientFactories) : ITunnelManager
{
    private readonly ConcurrentDictionary<string, Tunnel> tunnels = new();
    private readonly ITunnelClientFactory? tunnelClientFactory = tunnelClientFactories.MaxBy(f => f is ClientTunnelClientFactory);

    public async Task<Tunnel?> GetOrCreateTunnelAsync(string tunnelId, CancellationToken cancellationToken = default)
    {
        if (tunnels.TryGetValue(tunnelId, out var existingTunnel)) return existingTunnel;
        if (tunnelClientFactory == null) return null;
        var websocket = await tunnelClientFactory.CreateAsync();
        if (websocket == null) return null;
        var uri = await tunnelClientFactory.GetUriAsync();
        bool connected = false;
        while (!connected) {
            try
            {
                await websocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
                connected = true;
            }
            catch (TaskCanceledException)
            {
                websocket.Dispose();
                return null;
            }
            catch (WebSocketException ex) {
                logger.LogDebug("Websocket Error: {Uri}, {Message}", uri, ex.Message);
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                websocket = await tunnelClientFactory.CreateAsync() ?? throw new NullReferenceException("Websocket is null");
            }
        }
        logger.LogInformation("Connected to {Uri}", uri);
        var stream = MultiplexingStream.Create(websocket.AsStream(), new MultiplexingStream.Options
        {
            ProtocolMajorVersion = 3
        });
        var tunnel = new TunnelClient(websocket, stream) {Uri = uri};
        //TODO: Reconnect websocket if closed after initial connection if tunnel has not been disposed
        tunnel.Completion.ContinueWith(_ =>
        {
            logger.LogDebug("Removing tunnel {TunnelId}, uri: {Uri}", tunnelId, uri);
            return tunnels.TryRemove(new KeyValuePair<string, Tunnel>(tunnelId, tunnel));
        }, TaskScheduler.Default);
        return tunnels.GetOrAdd(tunnelId, tunnel);
    }

    public async Task StartTunnelAsync(HttpContext context, string tunnelId, CancellationToken cancellationToken = default)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await using var stream = await MultiplexingStream.CreateAsync(webSocket.AsStream(), new MultiplexingStream.Options {
            ProtocolMajorVersion = 3
        }, cancellationToken);
        var tunnel = new TunnelHost(webSocket, stream){Uri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}")};
        tunnels.AddOrUpdate(tunnelId, _ => tunnel, (_, oldTunnel) => {
            oldTunnel.Dispose();
            return tunnel;
        });
        logger.LogDebug("Tunnel connected: {TunnelId} from {RemoteIpAddress}:{RemotePort}", tunnelId, context.Connection.RemoteIpAddress, context.Connection.RemotePort);
        try
        {
            await stream.Completion;
        }
        catch (Exception e)
        {
            logger.LogDebug("Tunnel: {TunnelId}, Message: {Message}", tunnelId, e.Message);
        }
        finally
        {
            tunnels.TryRemove(new KeyValuePair<string, Tunnel>(tunnelId, tunnel));
        }
    }
}