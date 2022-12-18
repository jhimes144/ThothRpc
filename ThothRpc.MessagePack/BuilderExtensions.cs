using MessagePack;
using System;

namespace ThothRpc.MessagePack
{
    public static class BuilderExtensions
    {
        public static ClientHubBuilder UseMessagePack(this ClientHubBuilder builder)
        {
            ApplyHubConfiguration(builder.HubConfiguration);
            return builder;
        }

        public static ServerHubBuilder UseMessagePack(this ServerHubBuilder builder)
        {
            ApplyHubConfiguration(builder.HubConfiguration);
            return builder;
        }

        public static void ApplyHubConfiguration(HubConfiguration configuration)
        {
            configuration.ObjectSerializer = obj =>
            {
                if (obj == default)
                {
                    return Array.Empty<byte>();
                }

                return MessagePackSerializer.Serialize(obj);
            };

            configuration.ObjectDeserializer = (targetType, buffer) =>
            {
                if (buffer.IsEmpty)
                {
                    return default;
                }

                return MessagePackSerializer.Deserialize(targetType, buffer);
            };
        }
    }
}