# ThothRpc

ThothRpc is a holistic full duplex and bidirectional RPC framework for .Net. It's dead simple but powerful. It is completely platform agnostic and modular, making no assumptions of what kind of project you are building. The transport and object serialization (for parameters and method returns) layers are separate from the base library and custom implementation of these layers are easy to make giving you the freedom for you to easily build-your-own RPC system.

Of course, it wouldn’t be simple if these layers were not included for you. This library comes with a reliable-and-ordered UDP transport layer built off of [LiteNetLib](https://github.com/RevenantX/LiteNetLib) and a serialization layer built off of speedy [Message Pack](https://github.com/neuecc/MessagePack-CSharp) with a secure http/2 web based transport solution on the road map.

## Use Cases
Thoth (in its current state) is great for…
* Game multiplayer networking
* Ultra-fast reliable bi-directional microservice communication within a secured VPC
* LAN/VPN based apps and tools

Note: Currently encrypted secured traffic is not yet a feature but will be present in the upcoming http/2 transport. However, implementing your own encryption system is easy with our ingress and egress callbacks.

## Features

* Runtime based
  * No compile-time code generation
  * No contract files (.proto, ect)
  * Dynamic endpoint registration/unregistration
* Performant
  * Low GC pressure design
  * Low CPU Usage
  * Small packet size (down to 3 bytes total for an optimized fire and forget call)
* Various calling conventions
  * In-process direct method calls when client and server are on the same machine
  * Fast fire-and-forget calling server or client with customizable delivery mode
  * RPC bidirectional request-response calling
* Request handling customization for server and client separately
  * Manual handling allowing all incoming requests to polled on a thread (i.e game-loop)
  * Mulit-threaded thread pool handling of all incoming requests (like asp.net core)
* Holistic and modular
  * Configurable transport and serialization
  * Configurable data ingress and egress
  * No-dependency logging (works with whatever you have, just use the callbacks)
  * No middleware tie-ins, required dependency injection configuration, or complicated boilerplate code
