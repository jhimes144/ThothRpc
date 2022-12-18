using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    [Flags]
    internal enum PacketType : byte
    {
        IsMethodCall = 1 << 0,
        IsMethodCall_Optimized = 1 << 1,
        IsMethodCall_NoCallId = 1 << 2,
        IsMethodResponse = 1 << 3,
        IsMethodResponse_NoResult = 1 << 4,
        IsMethodResponse_NoException = 1 << 5,
        IsMethodResponse_Exception_InvalidCall = 1 << 6,
        IsMethodResponse_Exception_CallFailed = 1 << 7,
    }
}
