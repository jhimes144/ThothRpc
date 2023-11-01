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
        /// Indicates that requests will be handled manually via calling the <see cref="ServerHub.ProcessRequests"/> or <see cref="ClientHub.ProcessRequests"/> methods.
        /// This is best used for games where networking handling should be handled in a game loop or other thread you control.
        /// NOTE: Blocking request/response calls can cause deadlocks if you are running everything on a single thread.
        /// </summary>
        Manual,

        /// <summary>
        /// Indicates that requests will be handled on a thread pool (similar to ASP.Net core and other multithreaded servers).
        /// This mode provides ultimate scalability and the least restrictions, but can be overkill and should only be used by dedicated servers.
        /// </summary>
        MultiThreaded,

        /// <summary>
        /// Indicates that requests will be handled on a single background thread or async. This is great for clients, as it produces less overhead.
        /// NOTE: Calling a blocking request/response call in the context of a response from a call will cause dead locks in this mode.
        /// </summary>
        SingleThreaded,
    }
}
