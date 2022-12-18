using System;
using System.Net;

namespace ThothRpc
{
    public interface IPeerInfo
    {
        /// <summary>
        /// Id representing the peer. For servers, this is used to make calls to the client in particular.
        /// </summary>
        int PeerId { get; }

        /// <summary>
        /// The remote endpoint of the peer.
        /// </summary>
        IPEndPoint? RemoteEndpoint { get; }

        /// <summary>
        /// The underlying connection object, the nature of this object depends on the server/client implementation
        /// being used.
        /// </summary>
        object? UnderlyingConnection { get; }
    }
}