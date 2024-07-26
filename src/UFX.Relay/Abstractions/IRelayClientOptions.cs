using System.Net.WebSockets;
using UFX.Relay.Client;

namespace UFX.Relay.Abstractions;

public interface IRelayClientOptions
{
    Action<ClientWebSocketOptions>? WebSocketOptions { get; set; }
    Func<WebsocketEndpoint,string> GetRelayId { get; set; }
    public string RelayPathTemplate { get; set; }
}

