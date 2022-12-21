using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc
{
    /// <summary>
    /// Defines the configuration for a Hub.
    /// </summary>
    public interface IHubConfiguration
    {
        /// <summary>
        /// The maximum amount of time that the hub should wait for a response to a request before timing out.
        /// </summary>
        TimeSpan RequestTimeout { get; }

        /// <summary>
        /// Indicates whether or not the hub should suppress exceptions that are thrown during the execution of a request.
        /// </summary>
        bool SwallowExceptions { get; }

        /// <summary>
        /// Indicates whether or not the hub should return generic error messages when an exception occurs.
        /// </summary>
        bool GenericErrorMessages { get; }

        /// <summary>
        /// Indicates the strategy that the hub should use for handling incoming requests.
        /// </summary>
        RequestHandlingStrategy RequestHandlingStrategy { get; }

        /// <summary>
        /// The function that the hub should use to serialize objects.
        /// </summary>
        SerializeObject ObjectSerializer { get; }

        /// <summary>
        /// The function that the hub should use to deserialize objects.
        /// </summary>
        DeserializeObject ObjectDeserializer { get; }

        /// <summary>
        /// Optional function that the hub should use to transform incoming data.
        /// </summary>
        Func<byte[], byte[]>? DataIngressTransformer { get; }

        /// <summary>
        /// Optional function that the hub should use to transform outgoing data.
        /// </summary>
        Func<byte[], byte[]>? DataEgressTransformer { get; }
    }

    /// <summary>
    /// Delegate used for serializing parameters and return values.
    /// NOTE: obj may be null but the implementation must always return a byte array (empty in the case of null reference)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public delegate byte[] SerializeObject(object? obj);

    /// <summary>
    /// Delegate used for deserializing parameters and return values.
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public delegate object DeserializeObject(Type targetType, ReadOnlyMemory<byte> buffer);
}
