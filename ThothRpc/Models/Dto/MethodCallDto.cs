using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Models.Dto
{
    internal class MethodCallDto : IThothDto
    {
        /// <summary>
        /// Used only for functions
        /// </summary>
        public uint? CallId { get; set; }

        public string? ClassTarget { get; set; }

        public string? Method { get; set; }

        public List<ReadOnlyMemory<byte>> ArgumentsData { get; set; } 
            = new List<ReadOnlyMemory<byte>>();
    }
}