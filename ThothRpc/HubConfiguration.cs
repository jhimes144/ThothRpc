using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    /// <inheritdoc/>
    public class HubConfiguration : IHubConfiguration
    {
        /// <inheritdoc/>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(20);

        /// <inheritdoc/>
        public TimeSpan ConnectingTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <inheritdoc/>
        public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <inheritdoc/>
        public bool SwallowExceptions { get; set; }

        /// <inheritdoc/>
        public bool GenericErrorMessages { get; set; }

        /// <inheritdoc/>
        public RequestHandlingStrategy RequestHandlingStrategy { get; set; } = RequestHandlingStrategy.MultiThreaded;

        /// <inheritdoc/>
        public SerializeObject ObjectSerializer { get; set; }

        /// <inheritdoc/>
        public DeserializeObject ObjectDeserializer { get; set; }

        /// <inheritdoc/>
        public Func<byte[], byte[]>? DataIngressTransformer { get; set; }

        /// <inheritdoc/>
        public Func<byte[], byte[]>? DataEgressTransformer { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="HubConfiguration"/>
        /// </summary>
        /// <param name="objectSerializer">Function that the hub should use to serialize objects.</param>
        /// <param name="objectDeserializer">Function that the hub should use to deserialize objects.</param>
        public HubConfiguration(SerializeObject objectSerializer, DeserializeObject objectDeserializer)
        {
            ObjectSerializer = objectSerializer;
            ObjectDeserializer = objectDeserializer;
        }
    }
}
