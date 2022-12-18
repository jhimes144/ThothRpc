using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    /// <summary>
    /// Information regarding a peer.
    /// </summary>
    public class PeerInfo : IPeerInfo
    {
        /// <inheritdoc/>
        public int PeerId { get; set; }

        /// <inheritdoc/>
        public IPEndPoint? RemoteEndpoint { get; set; }

        /// <inheritdoc/>
        public object? UnderlyingConnection { get; set; }
    }
}
