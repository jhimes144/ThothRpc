using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    public interface IClient : IDisposable
    {
        void Init(IClientDelegator delegator, bool multiThreaded);

        ValueTask ConnectAsync(string address, int port, string connectionKey);
        void SendData(DeliveryMode deliveryMode, byte[] data);
        void ProcessRequests();
    }
}
