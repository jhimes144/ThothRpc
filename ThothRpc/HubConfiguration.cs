using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc
{
    public class HubConfiguration : IHubConfiguration
    {
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(20);

        public bool SwallowExceptions { get; set; }

        public bool GenericErrorMessages { get; set; }

        public RequestHandlingStrategy RequestHandlingStrategy { get; set; } = RequestHandlingStrategy.MultiThreaded;

        public SerializeObject ObjectSerializer { get; set; }

        public DeserializeObject ObjectDeserializer { get; set; }

        public Func<byte[], byte[]>? DataIngressTransformer { get; set; }

        public Func<byte[], byte[]>? DataEgressTransformer { get; set; }

        public HubConfiguration(SerializeObject objectSerializer, DeserializeObject objectDeserializer)
        {
            ObjectSerializer = objectSerializer;
            ObjectDeserializer = objectDeserializer;
        }
    }
}
