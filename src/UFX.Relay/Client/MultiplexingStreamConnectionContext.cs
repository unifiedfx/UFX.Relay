using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Nerdbank.Streams;

namespace UFX.Relay.Client;

public class MultiplexingStreamConnectionContext : ConnectionContext,
    IConnectionInherentKeepAliveFeature,
    // IConnectionEndPointFeature,
    IConnectionIdFeature,
    IConnectionItemsFeature,
    IConnectionLifetimeFeature,
    IConnectionTransportFeature
{
    private readonly CancellationTokenSource cts = new();
    private readonly MultiplexingStream.Channel channel;

    public MultiplexingStreamConnectionContext(string connectionId, MultiplexingStream.Channel channel, WebsocketEndpoint endpoint)
    {
        this.channel = channel;
        ConnectionId = connectionId;
        Transport = channel;
        this.channel.Completion.ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);
        Features.Set<IConnectionInherentKeepAliveFeature>(this);
        // Features.Set<IConnectionEndPointFeature>(this);
        Features.Set<IConnectionIdFeature>(this);
        Features.Set<IConnectionItemsFeature>(this);
        Features.Set<IConnectionLifetimeFeature>(this);
        Features.Set<IConnectionTransportFeature>(this);
    }

    public override string ConnectionId { get; set; }
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();
    public override IDuplexPipe Transport { get; set; }
    public bool HasInherentKeepAlive { get; }
    public override CancellationToken ConnectionClosed {
        get => cts.Token;
        set { }
    }
    public override void Abort(ConnectionAbortedException abortReason) => Abort();
    public override void Abort() {
        channel.Dispose();
        cts.Cancel();
    }
}