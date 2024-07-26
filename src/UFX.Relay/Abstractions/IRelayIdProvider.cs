namespace UFX.Relay.Abstractions;

public interface IRelayIdProvider {
    ValueTask<string?> GetRelayIdAsync(HttpContext context);
}