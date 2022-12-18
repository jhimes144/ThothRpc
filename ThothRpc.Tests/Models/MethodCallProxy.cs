using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Models.Dto;

namespace ThothRpc.Tests.Models
{
    internal class MethodCallProxy
    {
        public uint? CallId { get; set; }

        public string ClassTarget { get; set; }

        public string Method { get; set; }

        public List<object> Arguments { get; set; } = new List<object>();

        public byte[] Serialize(PacketAnalyzer analyzer, SerializeObject serializeObject)
        {
            var methodCallDto = new MethodCallDto
            {
                CallId = CallId,
                ClassTarget = ClassTarget,
                Method = Method,
                ArgumentsData = Arguments
                    .Select(a => new ReadOnlyMemory<byte>(serializeObject(a)))
                    .ToList()
            };

            return analyzer.SerializePacket(methodCallDto);
        }

        public static MethodCallProxy DeserializeFrom(byte[] bytes, PacketAnalyzer analyzer,
            DeserializeObject deserializeObject, params Type[] argumentTypes)
        {
            var methodCallDto = (MethodCallDto)analyzer.DeserializePacket(bytes);

            var proxy = new MethodCallProxy
            {
                CallId = methodCallDto.CallId,
                ClassTarget = methodCallDto.ClassTarget,
                Method = methodCallDto.Method
            };

            for (int i = 0; i < methodCallDto.ArgumentsData.Count; i++)
            {
                proxy.Arguments.Add(deserializeObject(argumentTypes[i], methodCallDto.ArgumentsData[i]));
            }

            return proxy;
        }

        
    }
}
