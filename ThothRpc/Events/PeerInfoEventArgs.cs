using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Events
{
    /// <summary>
    /// Event arguments that provide information about a peer.
    /// </summary>
    public class PeerInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the information about the peer.
        /// </summary>
        public IPeerInfo Peer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PeerInfoEventArgs"/> class.
        /// </summary>
        /// <param name="peer">The information about the peer.</param>
        public PeerInfoEventArgs(IPeerInfo peer)
        {
            Peer = peer;
        }
    }
}
