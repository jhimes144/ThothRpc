using LiteNetLib;
using LiteNetLib.Utils;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Models.Dto;
using ThothRpc.Utility;

namespace ThothRpc.LiteNetLib
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


    public abstract class LiteNetRpcManager : IDisposable
    {
        bool _isClient;
        bool _multiThreaded;

        protected readonly NetManager _manager;
        protected readonly EventBasedNetListener _listener = new EventBasedNetListener();
        protected string? _connectionKey;

        readonly Dictionary<int, IPeerInfo> _peersById
            = new Dictionary<int, IPeerInfo>();

        readonly ReaderWriterLockSlim _clientsLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets the underlying network manager. For advance use.
        /// </summary>
        public NetManager UnderlyingManager => _manager;

        public LiteNetRpcManager(bool isClient)
        {
            _manager = new NetManager(_listener);
            _isClient = isClient;

            _listener.NetworkReceiveEvent += onNetworkReceived;
            _listener.PeerConnectedEvent += onPeerConnected;

            // this can fire before ConnectionRequestEvent is fired for an invalid connection
            _listener.PeerDisconnectedEvent += onPeerDisconnected;

            if (!_isClient)
            {
                _listener.ConnectionRequestEvent += onConnectionRequest;
            }
        }

        protected void Init(bool multiThreaded)
        {
            _multiThreaded = multiThreaded;
            _manager.UnsyncedEvents = _multiThreaded;
        }

        private void onConnectionRequest(ConnectionRequest request)
        {
            if (!string.IsNullOrWhiteSpace(_connectionKey))
            {
                request.AcceptIfKey(_connectionKey);
            }
            else
            {
                request.Accept();
            }
        }

        void onPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            IPeerInfo? peerInfo = null;

            _clientsLock.EnterWriteLock();

            if (_peersById.TryGetValue(peer.Id, out peerInfo))
            {
                peerInfo = _peersById[peer.Id];
                _peersById.Remove(peer.Id);
            }

            _clientsLock.ExitWriteLock();

            if (peerInfo != null)
            {
                OnPeerDisconnected(peerInfo);
            }
        }

        void onPeerConnected(NetPeer peer)
        {
            _clientsLock.EnterWriteLock();

            var peerInfo = new PeerInfo
            {
                PeerId = peer.Id,
                RemoteEndpoint = peer.EndPoint,
                UnderlyingConnection = peer
            };

            _peersById.Add(peer.Id, new PeerInfo
            {
                PeerId = peer.Id,
                RemoteEndpoint = peer.EndPoint,
                UnderlyingConnection = peer
            });

            _clientsLock.ExitWriteLock();

            OnPeerConnected(peerInfo);
        }

        public virtual void Dispose()
        {
            _manager.DisconnectAll();
            _clientsLock.Dispose();
        }

        /// <inheritdoc/>
        public void ProcessRequests()
        {
            if (_multiThreaded)
            {
                throw new InvalidOperationException("This method cannot be called when" +
                    " configured for multithreaded request handling.");
            }

            _manager.PollEvents();
        }

        /// <inheritdoc/>
        public void SendData(int? peerId, DeliveryMode deliveryMode, byte[] data)
        {
            _clientsLock.EnterReadLock();

            if (peerId.HasValue)
            {
                if (!_peersById.TryGetValue(peerId.Value, out var peerInfo))
                {
                    throw new InvalidOperationException($"Peer by id {peerId} cannot be found.");
                }

                sendToPeer(peerInfo, deliveryMode, data);
            }
            else
            {
                if (_isClient)
                {
                    var peerInfo = _peersById.Values.FirstOrDefault();

                    if (peerInfo == null)
                    {
                        throw new InvalidOperationException("Not connected to server.");
                    }

                    sendToPeer(peerInfo, deliveryMode, data);
                }
                else
                {
                    foreach (var client in _peersById.Values)
                    {
                        sendToPeer(client, deliveryMode, data);
                    }
                }
            }

            _clientsLock.ExitReadLock();
        }

        void sendToPeer(IPeerInfo client, DeliveryMode mode, byte[] data)
        {
            var peer = (NetPeer)client.UnderlyingConnection!;
            peer.Send(data, (DeliveryMethod)mode);
        }

        void onNetworkReceived(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (_multiThreaded)
            {
                Task.Run(() =>
                {
                    processNetworkRecieve(peer, reader, deliveryMethod);
                    reader.Recycle();
                });
            }
            else
            {
                processNetworkRecieve(peer, reader, deliveryMethod);
                reader.Recycle();
            }
        }

        void processNetworkRecieve(NetPeer peer, NetPacketReader reader, DeliveryMethod _)
        {
            try
            {
                IPeerInfo? peerInfo = null;

                _clientsLock.EnterReadLock();
                // peer can be missing cuz of disconnect.
                peerInfo = _peersById[peer.Id];
                _clientsLock.ExitReadLock();

                OnDataReceived(peerInfo, reader.GetRemainingBytes());
            }
            catch (Exception e)
            {
                Logging.LogError($"Error processing incoming data {e}");
            }
        }

        protected abstract void OnDataReceived(IPeerInfo? peerInfo, byte[] data);

        protected abstract void OnPeerDisconnected(IPeerInfo peerInfo);
        protected abstract void OnPeerConnected(IPeerInfo peerInfo);
    }


#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
