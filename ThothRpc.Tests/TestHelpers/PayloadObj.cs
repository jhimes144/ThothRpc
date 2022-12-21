using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Tests.TestHelpers
{
    [MessagePackObject]
    public class PayloadObj
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public int Age { get; set; }
    }
}
