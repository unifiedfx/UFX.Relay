# UFX.Relay

UFX.Relay connects two ASPNet Core Middleware pipelines using a single WebSocket connection therefor extending a cloud application to an on-premise application instance.
This is similar to services like [ngrok](https://ngrok.com) but rather than requiring an external 3rd party service, UFX.Relay is a self-contained pure ASPNet Core solution.

The sample [Client](samples/Sample.Client/Program.cs) and [Server](samples/Sample.Server/Program.cs) projects demonstrate how to use UFX.Relay to connect a cloud application to an on-premise application with simple association of agents using a RelayId. 
A request to the server with a RelayId header will be forwarded to the corresponding client that connects with the same RelayId header.

The Server end of UFX.Relay leverages [YARP](https://github.com/microsoft/reverse-proxy) to forward ASPNet Core requests to the on-premise application via the WebSocket connection.
At the lowest level [YARP](https://github.com/microsoft/reverse-proxy) converts a HTTPContext to a HTTPClientRequest and sends it to the on-premise application via the WebSocket connection which uses a [MultiplexingStream](https://github.com/dotnet/Nerdbank.Streams/blob/main/doc/MultiplexingStream.md) to allow multiple requests to be sent over a single connection.
Note: This implementation uses [YARP DirectForwarding](https://github.com/microsoft/reverse-proxy/blob/main/src/ReverseProxy/Forwarder/HttpForwarder.cs) to forward requests to the on-premise application, any [YARP](https://github.com/microsoft/reverse-proxy) cluster configuration will not be used.

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
builder.WebHost.AddRelayListener("wss://localhost:7200");
```
This will create a Kestrel Listener that will inject requests (from the server) into the client ASPNet Core pipeline received over the WebSocket connection to the server (i.e. wss://localhost:7200) 

When a code based listener as added to Kestrel it will disable the use of the default Kestrel listener configuration derived from ASPNETCORE_URLS environment variable and -url command line argument.
If you require the default listener to be enabled you can set the includeDefaultUrls parameter to true as follows:

```csharp
builder.WebHost.AddRelayListener("wss://localhost:7200", includeDefaultUrls: true);
```

For simple association of agents using a static RelayId you can use the following configuration:

```csharp
builder.WebHost.AddRelayListener("wss://localhost:7200", options =>
{
    options.GetRelayId = _ => "123";
});
```

### Server

The minimal configuration for the server is as follows:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelayHost();
var app = builder.Build();
app.MapRelay();
app.Run();

```

Request sent to the server with a RelayId header will be forwarded to the corresponding client that connects with the same RelayId.
If a DefaultRelayId is set in the configuration then requests without a RelayId header will be forwarded to the client with the DefaultRelayId.
Combining this with the GetRelayId option in the client configuration allows for simple association of an agent using a static RelayId statically linking the client and server.

```csharp
builder.Services.AddRelayHost(options =>
{
    options.DefaultRelayId = "123";
});
var app = builder.Build();
app.MapRelay();
app.Run();

```

Note: AddRelayHost adds the relevant services to the DI container and MapRelay adds the Relay middleware to the application pipeline.

It is also possible to use a transformer (courtesy of [YARP](https://github.com/microsoft/reverse-proxy)) to modify the behaviour of the Relay middleware, the following example demonstrates how to use the transformer to enable the default forwarders (i.e. Forwarded-For):

```csharp
builder.Services.AddRelayHost(options =>
{
    options.Transformer = (transformBuilderContext) =>
    {
        transformBuilderContext.UseDefaultForwarders = true;
    };
});
var app = builder.Build();
app.MapRelay();
app.Run();

```

### Websocket Authentication

#### Client

The client WebSocket can be configured for authentication, for example setting an Authorization header to authenticate the WebSocket connection to the server as follows:

```csharp
builder.WebHost.AddRelayListener("wss://localhost:7200", options =>
{
    options.WebSocketOptions = socketOptions =>
    {
        socketOptions.SetRequestHeader("Authorization", "ApiKey 123");
    };
});
```

#### Server

The server can be configured to require Authentication for the WebSocket connection from the client using standard Minimal API middleware configuration as follows:

```csharp
app.MapRelay().RequireAuthorization();
```


## Future

* Connection Aggregation
  * The current implementation allows for an on-prem ASPNet Core instance to receive requests from a cloud ASPNet Core instance.
However, with some modification this could be inverted to allow for connection aggregation of multiple on-prem connections via a single connection to a cloud service.
The connection aggregation helps when there are a large number of idle connections (such as WebSockets) that need to be maintained.
[Azure Web PubSub](https://azure.microsoft.com/en-gb/products/web-pubsub) is an example of a cloud service that provides WebSocket connection aggregation.
Inverting the WebSocket connection direction of UFX.Relay would provide an equivalent capability to [Azure Web PubSub](https://azure.microsoft.com/en-gb/products/web-pubsub) but with the added benefit of being self-contained and not requiring a 3rd party service.

* Scaling across multiple instances of the cloud service could be achieved by using [Microsoft.Orleans](https://github.com/dotnet/orleans) to store the RelayId to instance mapping and redirect clients to the correct instance where the client is connected.

* Add an example of client certificate authentication for the WebSocket connection.
