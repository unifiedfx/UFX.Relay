namespace UFX.Relay.Tunnel;

public static class HttpContextExtensions
{
    private static readonly HashSet<string> excludedHosts = new() { "localhost", "api", "auth", "id", "user", "login", "www", "test", "dev", "staging", "prod", "production", "relay", "tunnel"};
    public static string? GetTunnelIdFromHost(this HttpContext context) => GetTunnelIdFromHost(context.Request.Host.Host);
    public static string? GetTunnelIdFromHost(this Uri uri)=> GetTunnelIdFromHost(uri.Host);
    private static string? GetTunnelIdFromHost(this string host)
    {
        var hostnameType = Uri.CheckHostName(host.ToLowerInvariant());
        if (hostnameType != UriHostNameType.Dns) return null;
        var parts = host.Split('.');
        return parts.Length <= 1 || excludedHosts.Contains(parts[0]) ? null : parts[0];
    }
}