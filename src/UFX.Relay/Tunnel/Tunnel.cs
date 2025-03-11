using System.Threading.Channels;
using Nerdbank.Streams;

namespace UFX.Relay.Tunnel;

public class Tunnel(MultiplexingStream stream) : IAsyncDisposable, IDisposable
{
    public Uri? Uri { get; set; }
    public Task Completion => stream?.Completion ?? Task.CompletedTask;
    private bool channelOfferedSubscribed;
    private readonly Channel<MultiplexingStream.Channel> channels = Channel.CreateUnbounded<MultiplexingStream.Channel>();
    private MultiplexingStream? stream = stream;

    public async Task<MultiplexingStream.Channel> GetChannelAsync(string? channelId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        if (channelId == null) return await GetChannelAsync(cancellationToken);
        var channel = await stream.OfferChannelAsync(channelId, cancellationToken);
        return channel;
    }
    public async Task<MultiplexingStream.Channel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        lock (channels)
        {
            if(!channelOfferedSubscribed) stream.ChannelOffered += StreamOnChannelOffered;
            channelOfferedSubscribed = true;
        }
        
        var channelResult = channels.Reader.ReadAsync(cancellationToken).AsTask();
        var streamCompletion = stream.Completion; 
#pragma warning disable VSTHRD003 // Waiting on (Completion) task outside context
        await Task.WhenAny(streamCompletion, channelResult);
#pragma warning restore VSTHRD003 // Waiting on (Completion) task outside context

        // 'stream.Completion' indicates that the underlying stream closed first:
        if (streamCompletion.IsCompleted) throw new UnderlyingStreamClosedException();
        
        return await channelResult;
    }

    private async void StreamOnChannelOffered(object? sender, MultiplexingStream.ChannelOfferEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));
        var channel = await stream.AcceptChannelAsync(e.Name);
        await channels.Writer.WriteAsync(channel);
    }

    public override string ToString() => (Uri?.ToString() ?? base.ToString())!;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || stream is null) return;
        stream.ChannelOffered -= StreamOnChannelOffered;
        if (stream is IDisposable disposable) disposable.Dispose();
        else
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            stream.DisposeAsync().AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }
        stream = null;
    }
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (stream is not null)
        {
            stream.ChannelOffered -= StreamOnChannelOffered;
            await stream.DisposeAsync().ConfigureAwait(false);
        }
        stream = null;
    }
}