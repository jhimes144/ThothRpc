using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.Tests.TestHelpers
{
    internal class MockServerClient : IClient, IServer
    {
        public byte[] DataSent { get; set; }
        public DeliveryMode DataSentDeliveryMode { get; set; }

        public ValueTask ConnectAsync(string address, int port, string connectionKey)
        {
            return default;
        }

        public void Dispose()
        {
        }

        public void Init(IServerDelegator delegator, bool multiThreaded)
        {
            
        }

        public void Init(IClientDelegator delegator, bool multiThreaded)
        {
            
        }

        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey)
        {
            
        }

        public void Listen(int port, string connectionKey)
        {
            
        }

        public void ProcessRequests()
        {
            
        }

        public void SendData(int? clientId, DeliveryMode deliveryMode, byte[] data)
        {
            DataSentDeliveryMode = deliveryMode;
            DataSent = data;
        }

        public void SendData(DeliveryMode deliveryMode, byte[] data)
        {
            DataSentDeliveryMode = deliveryMode;
            DataSent = data;
        }
    }
}
