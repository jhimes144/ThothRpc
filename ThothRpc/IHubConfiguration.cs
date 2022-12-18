using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    public interface IHubConfiguration
    {
        TimeSpan RequestTimeout { get; }

        bool SwallowExceptions { get; }

        bool GenericErrorMessages { get; }

        RequestHandlingStrategy RequestHandlingStrategy { get; }

        SerializeObject ObjectSerializer { get; }

        DeserializeObject ObjectDeserializer { get; }

        Func<byte[], byte[]>? DataIngressTransformer { get; }

        Func<byte[], byte[]>? DataEgressTransformer { get; }
    }

    /// <summary>
    /// Delegate used for serializing parameters and return values.
    /// NOTE: obj may be null but the implementation must always return a byte array (empty in the case of null reference)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public delegate byte[] SerializeObject(object? obj);


    public delegate object DeserializeObject(Type targetType, ReadOnlyMemory<byte> buffer);
}
