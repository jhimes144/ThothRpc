using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    public interface IClient : IDisposable
    {
        ConnectionState ConnectionState { get; }
        void Init(IClientDelegator delegator, TimeSpan connectingTimeout, bool multiThreaded);

        Task ConnectAsync(string address, int port, string connectionKey);
        void SendData(DeliveryMode deliveryMode, byte[] data);
        void Disconnect();
        void ProcessRequests();
    }
}
