using MessagePack;
using System;
using System.Buffers;

namespace ThothRpc.MessagePack
{
    public static class BuilderExtensions
    {
        public static ClientHubBuilder UseMessagePack(this ClientHubBuilder builder, MessagePackSerializerOptions options = null)
        {
            ApplyHubConfiguration(builder.HubConfiguration, options);
            return builder;
        }

        public static ServerHubBuilder UseMessagePack(this ServerHubBuilder builder, MessagePackSerializerOptions options = null)
        {
            ApplyHubConfiguration(builder.HubConfiguration, options);
            return builder;
        }

        public static void ApplyHubConfiguration(HubConfiguration configuration, MessagePackSerializerOptions options = null)
        {
            configuration.ObjectSerializer = obj =>
            {
                if (obj == default)
                {
                    return Array.Empty<byte>();
                }

                return MessagePackSerializer.Serialize(obj, options);
            };

            configuration.ObjectDeserializer = (targetType, buffer) =>
            {
                if (buffer.IsEmpty)
                {
                    return default;
                }

                return MessagePackSerializer.Deserialize(targetType, buffer, options);
            };
        }
    }
}