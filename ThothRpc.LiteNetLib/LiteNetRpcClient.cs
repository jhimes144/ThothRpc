using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.LiteNetLib
{
    public class LiteNetRpcClient : LiteNetRpcManager, IClient
    {
        IClientDelegator? _delegator;

        public LiteNetRpcClient() : base(true) { }

        public void Init(IClientDelegator delegator, bool multiThreaded)
        {
            _delegator = delegator;
            Init(multiThreaded);
        }

        public ValueTask ConnectAsync(string address, int port, string connectionKey)
        {
            _manager.Start();
            _manager.Connect(address, port, connectionKey);
            return default;
        }

        public void SendData(DeliveryMode deliveryMode, byte[] data)
        {
            SendData(null, deliveryMode, data);
        }

        public void SetDelegator(IClientDelegator delegator)
        {
            _delegator = delegator;
        }

        protected override void OnDataReceived(IPeerInfo? peerInfo, byte[] data)
        {
            _delegator?.OnDataReceivedAsync(data);
        }

        protected override void OnPeerConnected(IPeerInfo peerInfo)
        {
            _delegator?.OnConnected();
        }

        protected override void OnPeerDisconnected(IPeerInfo peerInfo)
        {
            _delegator?.OnDisconnected();
        }
    }
}
