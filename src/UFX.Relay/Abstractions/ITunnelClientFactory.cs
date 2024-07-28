using System.Net.WebSockets;

namespace UFX.Relay.Abstractions;

public interface ITunnelClientFactory
{
    ValueTask<ClientWebSocket?> CreateAsync();
    ValueTask<Uri> GetUriAsync();
}
