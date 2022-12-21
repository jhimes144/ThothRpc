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
        /// Information about the peer.
        /// </summary>
        public IPeerInfo Peer { get; }

        internal PeerInfoEventArgs(IPeerInfo peer)
        {
            Peer = peer;
        }
    }
}
