using System.Net.WebSockets;
using UFX.Relay.Abstractions;

namespace UFX.Relay.Client;

public class RelayClientOptions : IRelayClientOptions
{
    public Action<ClientWebSocketOptions>? WebSocketOptions { get; set; }
    public Func<WebsocketEndpoint,string> GetRelayId { get; set; } = _ => Guid.NewGuid().ToString("N");
    public string RelayPathTemplate { get; set; } = "/relay/{0}";
}