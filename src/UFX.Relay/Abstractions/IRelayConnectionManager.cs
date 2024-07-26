namespace UFX.Relay.Abstractions;

public interface IRelayConnectionManager {
    Task AddWebSocket(HttpContext context, string relayId);
    Task<Stream> GetStreamAsync(HttpContext context);
    ValueTask<bool> CanForward(HttpContext context);
}