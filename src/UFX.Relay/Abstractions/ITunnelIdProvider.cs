
namespace UFX.Relay.Abstractions;

public interface ITunnelIdProvider {
    ValueTask<string?> GetTunnelIdAsync();
}