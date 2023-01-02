using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    /// <summary>
    /// Connection state of a client hub.
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// The client is disconnected from the server.
        /// </summary>
        Disconnected,

        /// <summary>
        /// The client is currently attempting to connect.
        /// </summary>
        Connecting,

        /// <summary>
        /// The client is connected.
        /// </summary>
        Connected
    }
}
