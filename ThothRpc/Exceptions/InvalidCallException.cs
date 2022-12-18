using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Exceptions
{
	/// <summary>
	/// Exception thrown when a method does not exist on remote, or the signature does not match.
	/// </summary>
	[Serializable]
	public class InvalidCallException : Exception
	{
		internal InvalidCallException() { }
        internal InvalidCallException(string message) : base(message) { }
        internal InvalidCallException(string message, Exception inner) : base(message, inner) { }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected InvalidCallException(
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
          System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}
}
