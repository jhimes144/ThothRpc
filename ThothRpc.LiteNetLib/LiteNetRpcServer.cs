using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.LiteNetLib
{
    public class LiteNetRpcServer : LiteNetRpcManager, IServer
    {
        IServerDelegator? _delegator;

        public LiteNetRpcServer() : base(false) { }

        public void Init(IServerDelegator delegator, bool multiThreaded)
        {
            _delegator = delegator;
            Init(multiThreaded);
        }

        /// <inheritdoc/>
        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey)
        {
            _connectionKey = connectionKey;
            _manager.Start(addressIPv4, addressIPv6, port);
        }

        /// <inheritdoc/>
        public void Listen(int port, string connectionKey)
        {
            _connectionKey = connectionKey;
            _manager.Start(port);
        }

        protected override void OnDataReceived(IPeerInfo? peerInfo, byte[] data)
        {
            if (peerInfo != null)
            {
                _delegator?.OnDataReceivedAsync(peerInfo, data);
            }
            else
            {
                throw new Exception("Server expects peerInfo to be specified.");
            }
        }

        protected override void OnPeerDisconnected(IPeerInfo peerInfo)
        {
            _delegator?.OnPeerDisconnected(peerInfo);
        }

        protected override void OnPeerConnected(IPeerInfo peerInfo)
        {
            _delegator?.OnPeerConnected(peerInfo);
        }
    }
}
