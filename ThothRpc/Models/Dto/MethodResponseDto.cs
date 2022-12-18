using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Exceptions;

namespace ThothRpc.Models.Dto
{
    internal class MethodResponseDto : IThothDto
    {
        public uint CallId { get; set; }

        public ReadOnlyMemory<byte>? ResultData { get; set; }

        public ExceptionContainer? Exception { get; set; }
    }
}
