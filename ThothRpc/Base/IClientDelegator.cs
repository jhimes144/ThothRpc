using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    public interface IClientDelegator
    {
        void OnConnected();
        void OnDisconnected();
        ValueTask OnDataReceivedAsync(byte[] data);
    }
}
