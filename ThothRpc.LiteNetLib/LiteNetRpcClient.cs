using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Utility;

namespace ThothRpc.LiteNetLib
{
    public class LiteNetRpcClient : LiteNetRpcManager, IClient
    {
        IClientDelegator? _delegator;
        readonly object _connectionStateLock = new object();

        TaskCompletionSource<bool> _connectionCompletionSource;
        CancellationTokenSource _connectionTimeoutCancelSrc;
        ConnectionState _connectionState;
        TimeSpan _connectingTimeout = TimeSpan.FromSeconds(5);

        public ConnectionState ConnectionState
        {
            get
            {
                lock (_connectionStateLock)
                {
                    return _connectionState;
                }
            }
            set
            {
                lock (_connectionStateLock)
                {
                    _connectionState = value;
                }
            }
        }

        public LiteNetRpcClient() : base(true) { }

        public void Init(IClientDelegator delegator, TimeSpan connectingTimeout, bool multiThreaded)
        {
            _delegator = delegator;
            _connectingTimeout = connectingTimeout;
            Init(multiThreaded);
        }

        public async Task ConnectAsync(string address, int port, string connectionKey)
        {
            lock (_connectionStateLock)
            {
                if (_connectionState != ConnectionState.Disconnected)
                {
                    Disconnect();
                }

                _connectionState = ConnectionState.Connecting;
                _connectionCompletionSource = new TaskCompletionSource<bool>();
                _connectionTimeoutCancelSrc?.Dispose();
                _connectionTimeoutCancelSrc = new CancellationTokenSource();

                _connectionTimeoutCancelSrc.Token.Register(() =>
                {
                    lock (_connectionStateLock)
                    {
                        if (_connectionState == ConnectionState.Connecting)
                        {
                            _connectionCompletionSource.TrySetException(new TimeoutException("Connection attempt timed-out."));
                        }

                        _connectionState = ConnectionState.Disconnected;
                    }
                });

                _connectionTimeoutCancelSrc.CancelAfter(_connectingTimeout);
            }

            await Task.Run(() =>
            {
                _manager.Stop();
                _manager.Start();
                _manager.Connect(address, port, connectionKey);
            });

            await _connectionCompletionSource.Task;
        }

        public void Disconnect()
        {
            _manager.Stop();

            lock (_connectionStateLock)
            {
                _connectionState = ConnectionState.Disconnected;
            }
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
            if (_delegator != null)
            {
                _delegator?.OnDataReceivedAsync(data);
            }
            else
            {
                throw new InvalidOperationException("Expected _delegator not to be null");
            }
        }

        protected override void OnPeerConnected(IPeerInfo peerInfo)
        {
            lock (_connectionStateLock)
            {
                if (_connectionState == ConnectionState.Connecting)
                {
                    _connectionCompletionSource.SetResult(true);
                }

                _connectionState = ConnectionState.Connected;
            }

            _delegator?.OnConnected();
        }

        protected override void OnPeerDisconnected(IPeerInfo peerInfo)
        {
            lock (_connectionStateLock)
            {
                _connectionState = ConnectionState.Disconnected;
            }

            _delegator?.OnDisconnected();
        }
    }
}
