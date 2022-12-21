using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.Tests.TestHelpers
{
    internal class TestClient : IClient
    {
        IClientDelegator _clientDelegator;

        public TestServer Server { get; set; }

        public ValueTask ConnectAsync(string address, int port, string connectionKey)
        {
            return default;
        }

        public void Dispose() { }

        public void Init(IClientDelegator delegator, bool multiThreaded)
        {
            _clientDelegator = delegator;
        }

        public void ProcessRequests() { }

        public void SendData(DeliveryMode deliveryMode, byte[] data)
        {
            Server.ReceiveData(this, data);
        }

        public void ReceiveData(byte[] data)
        {
            _clientDelegator.OnDataReceivedAsync(data);
        }
    }
}
