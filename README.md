# UFX.Relay

UFX.Relay connects two ASPNet Core Middleware pipelines using a single WebSocket connection therefor extending a cloud application to an on-premise application instance.
This is similar to services like [ngrok](https://ngrok.com) but rather than requiring an external 3rd party service, UFX.Relay is a self-contained pure ASPNet Core solution.

The sample [Client](samples/Sample.Client/Program.cs) and [Server](samples/Sample.Server/Program.cs) projects demonstrate how to use UFX.Relay to connect a cloud application to an on-premise application with simple association of agents using a TunnelId. 
A request to the server/forwarder with a TunnelId header will be forwarded to the corresponding client/listener that connects with the same TunnelId.

The Server/Forwarder end of UFX.Relay leverages [YARP](https://github.com/microsoft/reverse-proxy) to forward ASPNet Core requests to the on-premise application via the WebSocket connection.
At the lowest level [YARP](https://github.com/microsoft/reverse-proxy) converts a HTTPContext to a HTTPClientRequest and sends it to the on-premise application via the WebSocket connection which uses a [MultiplexingStream](https://github.com/dotnet/Nerdbank.Streams/blob/main/doc/MultiplexingStream.md) to allow multiple requests to be sent over a single connection.
Note: This implementation uses [YARP DirectForwarding](https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/HttpForwarder.cs) to forward requests to the on-premise application, any [YARP](https://github.com/microsoft/reverse-proxy) cluster configuration will not be used.

## Overview

UFX.Relay comprises three components:
* Forwarder
* Listener
* Tunnel

### Forwarder
This uses [YARP DirectForwarding](https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/HttpForwarder.cs) to forward requests over the tunnel connection to be received by the listener.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelForwarder();
var app = builder.Build();
app.MapTunnelForwarder();
app.Run();
```

### Listener
The listener received requests over the tunnel from the forwarder and injects them into the ASPNet Core pipeline.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.AddTunnelListener(options =>
{
    options.DefaultTunnelId = "123";
});
```

### Tunnel
The Tunnel is a logical layer on top of a WebSocket connection that allows for multiple requests to be multiplexed over a single connection.
The tunnel has both a Client and Host end, the Forwarder and Listener can use either the Tunnel Client or Tunnel Host. Typically, the forwarder would be used with the Tunnel Host and the listener with the Tunnel Client for a ngrok replacement scenario.
However, if the Tunnel Client and Host are swapped this would allow for connection aggregation of multiple on-prem connections via a single connection to a cloud service.


#### Tunnel Client

The client requires the TunnelHost and TunnelId to be specified in order to connect to the Tunnel Host.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelClient(options =>
{
    options.TunnelHost = "wss://localhost:7200";
    options.TunnelId = "123";
});
```

#### Tunnel Host

The tunnel host is added as a minimal api endpoint to the application pipeline accepting websocket connections on /tunnel/{tunnelId} by default.

```csharp
var app = builder.Build();
app.MapTunnelHost();
app.Run();
```


## Sample Projects

The sample projects demonstrate how to use UFX.Relay to connect a cloud application to an on-premise application with simple association of agents using a static RelayId, this in effect creates a static tunnel between the client and server.
Once the sample [Client](samples/Sample.Client/Program.cs) and [Server](samples/Sample.Server/Program.cs) projects have started requests to https://localhost:7200/ will be forwarded to the client application.

The sample server hosts on https://localhost:7200/ and the client hosts on https://localhost:7100.

The sample client opens a websocket connection to the server using wss://localhost:7200/relay/123

Example responses can be tested as follows:

A request to https://localhost:7200/server is handled by the server and a request to https://localhost:7200/client is forwarded to the client and returned via the server.

## Configuration

### Client

The minimal configuration for the client is as follows:

```csharp
builder.WebHost.AddTunnelListener(options => { options.DefaultTunnelId = "123"; });
builder.Services.AddTunnelClient(options => { options.TunnelHost = "wss://localhost:7200"; });
```
This will create a Kestrel Listener that will inject requests (from the forwarder) into the client ASPNet Core pipeline received over the WebSocket connection to the server (i.e. wss://localhost:7200) 

When a code based listener is added to Kestrel it will disable the use of the default Kestrel listener configuration derived from ASPNETCORE_URLS environment variable and -url command line argument.
If you require the default listener to be enabled you can set the includeDefaultUrls parameter to true as follows:

```csharp
builder.WebHost.AddTunnelListener(options =>
{
    options.DefaultTunnelId = "123";
}, includeDefaultUrls: true);
```
The sample uses a simple association of agents using a static TunnelId '123'

### Server

The minimal configuration for the server is as follows:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelForwarder();
var app = builder.Build();
app.MapTunnelHost();
app.Run();

```

Request sent to the server with a TunnelId header will be forwarded to the corresponding listener that connects with the same TunnelId.
If a DefaultTunnelId is set in the configuration then requests without a TunnelId header will be forwarded to the listener with the DefaultTunnelId.
Combining this with the DefaultTunnelId option in the listener configuration allows for simple association of an agent using a static TunnelId statically linking the forwarder and listener.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelForwarder(options =>
{
    options.DefaultTunnelId = "123";
});
builder.Services.AddRelayHost();
var app = builder.Build();
app.MapTunnelHost();
app.Run();

```

It is also possible to use a transformer (courtesy of [YARP](https://github.com/microsoft/reverse-proxy)) to modify the behaviour of the Forwarder, the following example demonstrates how to use the transformer to enable the default forwarders (i.e. Forwarded-For):

```csharp
builder.Services.AddTunnelForwarder(options =>
{
    options.Transformer = transformBuilderContext =>
    {
        transformBuilderContext.UseDefaultForwarders = true;
    };
});
```

### Websocket Authentication

#### Client

The client WebSocket can be configured for authentication, for example setting an Authorization header to authenticate the WebSocket connection to the server as follows:

```csharp
builder.Services.AddTunnelClient(options =>
{
    options.WebSocketOptions = socketOptions =>
    {
        socketOptions.SetRequestHeader("Authorization", "ApiKey 123");
    };
    options.TunnelHost = "wss://localhost:7200";
    options.TunnelId = "123";
});
```

#### Server

The Tunnel Host can be configured to require Authentication for the WebSocket connection from the Tunnel Client using standard Minimal API middleware configuration as follows:

```csharp
app.MapTunnelHost().RequireAuthorization();
```

## Connection Aggregation
Connection aggregation helps when there are a large number of idle connections (such as WebSockets) that need to be maintained.
[Azure Web PubSub](https://azure.microsoft.com/en-gb/products/web-pubsub) is an example of a cloud service that provides WebSocket connection aggregation.
Inverting the WebSocket connection direction of UFX.Relay provides an equivalent capability to [Azure Web PubSub](https://azure.microsoft.com/en-gb/products/web-pubsub) but with the added benefit of being self-contained and not requiring a 3rd party service.
Typically, the Forwarder would be hosted on the cloud and the listener on-prem, this allows for the cloud application to connect to the on-prem application.
However, it is possible to have the forwarder on-prem and the listener in the cloud while still using an out-bound WebSocket connection from the on-prem instance to the cloud thus allowing for connection aggregation of multiple on-prem connections via a single connection to a cloud service.

### Aggregation Cloud End Example

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.AddTunnelListener(options =>{ options.DefaultTunnelId = "123"; });
var app = builder.Build();
app.MapTunnelHost();
app.Run();
```

### Aggregation On-Prem End Example

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTunnelForwarder(options => { options.DefaultTunnelId = "123"; });
builder.Services.AddTunnelClient(options =>
{
    options.TunnelHost = "wss://localhost:7100";
    options.TunnelId = "123";
});    
var app = builder.Build();
app.MapTunnelForwarder();
app.Run();
```

## Future

* Scaling across multiple instances of the cloud service could be achieved by using [Microsoft.Orleans](https://github.com/dotnet/orleans) to store the TunnelId to instance mapping and redirect clients to the correct instance where the client is connected.
* Add an example of client certificate authentication for the WebSocket connection.
* Consider adding TCP/UDP Forwarding over the tunnel