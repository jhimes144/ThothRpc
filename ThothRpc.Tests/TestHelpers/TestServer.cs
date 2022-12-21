using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.Tests.TestHelpers
{
    internal class TestServer : IServer
    {
        readonly Dictionary<int, TestClient> _clientsById = new();
        IServerDelegator _serverDelegator;

        public void Dispose() { }

        public void Init(IServerDelegator delegator, bool multiThreaded)
        {
            _serverDelegator = delegator;
        }

        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey) { }

        public void Listen(int port, string connectionKey) { }

        public void ProcessRequests() { }

        public void AddClient(TestClient client) 
        {
            _clientsById.Add(_clientsById.Count + 1, client);
            client.Server = this;
        }

        public void SendData(int? clientId, DeliveryMode deliveryMode, byte[] data)
        {
            if (clientId == -1)
            {
                throw new Exception("I should never see a -1 client id.");
            }

            if (clientId.HasValue)
            {
                if (_clientsById.TryGetValue(clientId.Value, out TestClient client))
                {
                    client.ReceiveData(data);
                }
            }
            else
            {
                foreach (var client in _clientsById.Values)
                {
                    client.ReceiveData(data);
                }
            }
        }

        public void ReceiveData(TestClient client, byte[] data)
        {
            if (_clientsById.Values.Contains(client))
            {
                var id = _clientsById.First(p => p.Value == client).Key;
                _serverDelegator.OnDataReceivedAsync(new PeerInfo
                {
                    PeerId = id,
                }, data);
            }
        }
    }
}
