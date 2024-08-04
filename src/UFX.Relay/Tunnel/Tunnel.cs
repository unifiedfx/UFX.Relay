using System.Threading.Channels;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

public class Tunnel(MultiplexingStream stream) : IDisposable
{
    public Uri? Uri { get; set; }
    private bool channelOfferedSubscribed;
    private readonly Channel<MultiplexingStream.Channel> channels = Channel.CreateUnbounded<MultiplexingStream.Channel>();
    public async Task<MultiplexingStream.Channel> GetChannelAsync(string? channelId, CancellationToken cancellationToken = default)
    {
        if (channelId == null) return await GetChannelAsync(cancellationToken);
        var channel = await stream.OfferChannelAsync(channelId, cancellationToken);
        return channel;
    }
    public async Task<MultiplexingStream.Channel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        lock (channels)
        {
            if(!channelOfferedSubscribed) stream.ChannelOffered += StreamOnChannelOffered;
            channelOfferedSubscribed = true;
        }
        return await channels.Reader.ReadAsync(cancellationToken);
    }

    private async void StreamOnChannelOffered(object? sender, MultiplexingStream.ChannelOfferEventArgs e)
    {
        var channel = await stream.AcceptChannelAsync(e.Name);
        await channels.Writer.WriteAsync(channel);
    }

    public virtual void Dispose() {
        stream.ChannelOffered -= StreamOnChannelOffered;
        stream.Dispose();
    }
    public override string ToString() => Uri?.ToString() ?? base.ToString();
}