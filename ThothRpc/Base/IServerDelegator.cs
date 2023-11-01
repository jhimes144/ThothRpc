using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    /// <summary>
    /// Represents the mechanism of which a network server can communicate upstream to a server hub.
    /// </summary>
    public interface IServerDelegator
    {
        /// <summary>
        /// Called when a new client connects to the server.
        /// </summary>
        /// <param name="client">The information about the connected client.</param>
        void OnPeerConnected(IPeerInfo client);

        /// <summary>
        /// Called when a client disconnects from the server.
        /// </summary>
        /// <param name="client">The information about the disconnected client.</param>
        void OnPeerDisconnected(IPeerInfo client);

        // data must be a free instance, no other users
        ValueTask OnDataReceivedAsync(IPeerInfo client, ReadOnlyMemory<byte> data);
    }
}
