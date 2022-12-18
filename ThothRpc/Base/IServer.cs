using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThothRpc.Base
{
    public interface IServer : IDisposable
    {
        void Init(IServerDelegator delegator, bool multiThreaded);

        void SendData(int? clientId, DeliveryMode deliveryMode, byte[] data);
        void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey);
        void Listen(int port, string connectionKey);
        void ProcessRequests();
    }
}
