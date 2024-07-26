using System.Net;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;
using Nerdbank.Streams;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Client;

public class RelayConnectionListener(WebsocketEndpoint websocketEndpoint, IRelayClientOptions clientOptions) : IConnectionListener
{
    private Channel<MultiplexingStreamConnectionContext> contexts = Channel.CreateUnbounded<MultiplexingStreamConnectionContext>();
    private MultiplexingStream? stream;
    public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = new CancellationToken()) => await contexts.Reader.ReadAsync(cancellationToken);

    public async Task Bind()
    {
        //TODO: This needs to return without waiting for the websocket to connect
        // otherwise the server will block all listners until the client connects
        contexts = Channel.CreateUnbounded<MultiplexingStreamConnectionContext>();
        stream = await CreateRelayStreamAsync(websocketEndpoint, CancellationToken.None);
        stream.ChannelOffered += StreamOnChannelOffered;
        stream.StartListening();
    }

    public async ValueTask UnbindAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        if (stream == null) return;
        stream.ChannelOffered -= StreamOnChannelOffered;
        contexts.Writer.TryComplete();
        await stream.DisposeAsync();
        stream = null;
    }

    public EndPoint EndPoint { get; } = websocketEndpoint;
    
    public ValueTask DisposeAsync() => stream?.DisposeAsync() ?? new ValueTask();

    private async void StreamOnChannelOffered(object? sender, MultiplexingStream.ChannelOfferEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var channel = await stream.AcceptChannelAsync(e.Name);
        var context = new MultiplexingStreamConnectionContext(e.Name, channel, websocketEndpoint);
        await contexts.Writer.WriteAsync(context);
    }
    private async ValueTask<MultiplexingStream> CreateRelayStreamAsync(WebsocketEndpoint endpoint, CancellationToken cancellationToken)
    {
        var websocket = endpoint.CreateClient(clientOptions);
        var wsUri = new UriBuilder(endpoint.Uri);
        bool connected = false;
        while (!connected) {
            try {
                websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(5);
                wsUri.Path = string.Format(clientOptions.RelayPathTemplate, clientOptions.GetRelayId(endpoint));
                await websocket.ConnectAsync(wsUri.Uri, cancellationToken).ConfigureAwait(false);
                connected = true;
            }
            catch (WebSocketException ex) {
                Console.WriteLine("Websocket Error: {0}, {1}", endpoint.Uri, ex.Message);
                await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                websocket = endpoint.CreateClient(clientOptions);
            }
        }
        Console.WriteLine("Connected to {0}", wsUri);
        //TODO: Reconnect websocket if closed after initial connection until unbind is called
        return MultiplexingStream.Create(websocket.AsStream(), new MultiplexingStream.Options()
        {
            ProtocolMajorVersion = 3,
            StartSuspended = true
        });
    }
}