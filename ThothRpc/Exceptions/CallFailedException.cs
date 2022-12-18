using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Exceptions
{
    /// <summary>
    /// Exception raised when an rpc call caused an unhandled exception on the remote host.
    /// For such exception to be raised, the remote must be configured to raise CallFailedExceptions
    /// </summary>
    [Serializable]
	public class CallFailedException : Exception
	{
        internal CallFailedException() { }
		internal CallFailedException(string message) : base(message) { }
        internal CallFailedException(string message, Exception inner) : base(message, inner) { }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected CallFailedException(
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
          System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
