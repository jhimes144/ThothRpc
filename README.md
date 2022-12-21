# ThothRpc

ThothRpc is a drop-in, holistic, lightweight, full duplex and bidirectional RPC framework for .Net. It's dead simple but powerful. It is completely platform agnostic and modular, making no assumptions of what kind of project you are building. The transport and object serialization (for parameters and method returns) layers are separate from the base library and custom implementation of these layers are easy to make giving you the freedom for you to easily build-your-own RPC system.

Of course, it wouldn’t be simple if these layers were not included for you. This library comes with a reliable-and-ordered UDP transport layer built off of [LiteNetLib](https://github.com/RevenantX/LiteNetLib) and a serialization layer built off of speedy [Message Pack](https://github.com/neuecc/MessagePack-CSharp) with a secure http/2 web based transport solution on the road map.

## Usage Examples

### Typed Rpc
#### Shared Code
``` csharp
public interface IClientService
{
    [ThothMethod] // indicates that this method is callable from server
    void PrintServerTime(DateTime time);

    Task GetHelloWorld();
}

public interface IServerService
{
    [ThothMethod] // indicates that this method is callable from client
    string GetHelloWorld();
}
```
#### Server
``` csharp
var hub = ServerHubBuilder.BuildServer()
    .UseTransport<LiteNetRpcServer>()
    .UseMessagePack()
    .Build();

var serverService = new ServerService(hub);
hub.RegisterAs<IServerService>(serverService);
hub.Listen(9050, "SomeConnectionKey");

public class ServerService : IServerService
{
    readonly ServerHub _hub;

    public ServerService(ServerHub hub)
    {
        _hub = hub;

        Task.Run(async () =>
        {
            while (true)
            {
                var now = DateTime.Now;
                
                // Fire and forget
                _hub.InvokeForgetAllClients<IClientService>(DeliveryMode.Sequenced,
                    c => c.PrintServerTime(now));

                await Task.Delay(1000);
            }
        });
    }

    public string GetHelloWorld() // called from client
    {
        return "Hello World From Server!";
    }
}
```
#### Client
``` csharp
var hub = ClientHubBuilder.BuildClient()
    .UseTransport<LiteNetRpcClient>()
    .UseMessagePack()
    .Build();

var clientService = new ClientService(hub);
hub.RegisterAs<IClientService>(clientService);

await hub.ConnectAsync("localhost", 9050, "SomeConnectionKey");
await clientService.GetHelloWorld();

public class ClientService : IClientService
{
    readonly ClientHub _hub;

    public ClientService(ClientHub hub)
    {
        _hub = hub;
    }

    public async Task GetHelloWorld()
    {
        // Method invocations not using fire-and-forget with a udp transport are always delivered reliable and ordered.
        var helloWorld = await _hub.InvokeServerAsync<IServerService, string>
            (s => s.GetHelloWorld());

        Console.WriteLine(helloWorld);
    }

    public void PrintServerTime(DateTime time) // called from server
    {
        Console.WriteLine($"Server time: {time}");
    }
}
```
### Typeless Rpc
#### Server
``` csharp
var hub = ServerHubBuilder.BuildServer()
    .UseTransport<LiteNetRpcServer>()
    .UseMessagePack()
    .Build();

var serverService = new ServerService(hub);
hub.Register(serverService, "ServerService");
hub.Listen(9050, "SomeConnectionKey");

public class ServerService
{
    readonly ServerHub _hub;

    public ServerService(ServerHub hub)
    {
        _hub = hub;

        Task.Run(async () =>
        {
            while (true)
            {
                _hub.InvokeForgetAllClients(DeliveryMode.Sequenced, 
                    "ClientService", "PrintServerTime", DateTime.Now);

                await Task.Delay(1000);
            }
        });
    }

    [ThothMethod]
    public string GetHelloWorld()
    {
        return "Hello World From Server!";
    }
}
```
#### Client
``` csharp
var hub = ClientHubBuilder.BuildClient()
    .UseTransport<LiteNetRpcClient>()
    .UseMessagePack()
    .Build();

var clientService = new ClientService(hub);
hub.Register(clientService, "ClientService");

await hub.ConnectAsync("localhost", 9050, "SomeConnectionKey");
await clientService.GetHelloWorld();

public class ClientService
{
    readonly ClientHub _hub;

    public ClientService(ClientHub hub)
    {
        _hub = hub;
    }

    public async Task GetHelloWorld()
    {
        var helloWorld = await _hub.InvokeServerAsync<string>
            ("ServerService", "GetHelloWorld");

        Console.WriteLine(helloWorld);
    }

    [ThothMethod]
    public void PrintServerTime(DateTime time)
    {
        Console.WriteLine($"Server time: {time}");
    }
}
```

## Use Cases
Thoth (in its current state) is great for…
* Game multiplayer networking
* Ultra-fast reliable bi-directional microservice communication within a secured VPC
* LAN/VPN based apps and tools

Note: Currently encrypted secured traffic is not yet a feature but will be present in the upcoming http/2 transport. However, implementing your own encryption system is easy with the ingress and egress callbacks.

## Features

* Runtime based
  * No contract files (.proto, ect)
  * Dynamic endpoint registration/unregistration
* Performant
  * Low GC pressure design
  * Low CPU Usage
  * Small packet size (down to 3 bytes total for an optimized fire and forget call)
* Various calling conventions
  * Typed or typeless invocation
  * In-process direct method calls when client and server are on the same machine
  * Fast fire-and-forget calling server or client with customizable delivery mode
  * Reliable RPC bi-directional request-response calling
* Request handling customization for server and client separately
  * Manual handling allowing all incoming requests to be polled on a thread (i.e game-loop)
  * Multi-threaded thread pool handling of all incoming requests (like asp.net core)
* Holistic and modular
  * Configurable transport and serialization
  * Configurable data ingress and egress
  * No-dependency logging (works with whatever you have, just use the callbacks)
  * No middleware tie-ins, required dependency injection configuration, or complicated boilerplate code
