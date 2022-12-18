using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    /// <summary>
    /// Describes how a Hub will process incoming method calls.
    /// </summary>
    public enum RequestHandlingStrategy
    {
        /// <summary>
        /// Indicates that the hub will process requests when <see cref="ClientHub.ProcessRequests"/> or <see cref="ServerHub.ProcessRequests"/>
        /// is called. This allows request processing to happen on a thread you control. This is useful for applications with a main loop
        /// (such as a game).
        /// </summary>
        Manual,

        /// <summary>
        /// Indicates that the hub will process requests in a thread pool. This is similar to how most web frameworks process requests.
        /// </summary>
        MultiThreaded
    }
}
