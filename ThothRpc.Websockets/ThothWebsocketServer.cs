using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.Websockets
{
    public class ThothWebsocketServer : IServer
    {
        int _newId = 0;
        IServerDelegator? _delegator;
        readonly CancellationTokenSource _stopClcSrc = new CancellationTokenSource();
        readonly Dictionary<int, WebSocketPeer> _peersById = new Dictionary<int, WebSocketPeer>();

        /// <summary>
        /// Adds a peer using the given websocket. Returns a task that completes when the peer is closed.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        public Task AddPeerAsync(WebSocket socket, IPEndPoint endpoint)
        {
            _newId++;

            var peer = new WebSocketPeer(_newId, endpoint, socket);
            _peersById.Add(_newId, peer);
            _delegator.OnPeerConnected(peer);

            receiveLoop(peer);
            return peer.DisposeInitiatedTcs.Task;
        }

        public IReadOnlyDictionary<int, IPeerInfo> GetPeers()
        {
            return _peersById.ToDictionary(p => p.Key, p => (IPeerInfo)p.Value);
        }

        public void Init(IServerDelegator delegator, RequestHandlingStrategy requestHandling, TimeSpan disconnectTimeout)
        {
            if (requestHandling != RequestHandlingStrategy.SingleThreaded)
            {
                throw new NotSupportedException("Only SingleThreaded strategy is supported.");
            }

            _delegator = delegator;
        }

        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey)
        {
            throw new NotSupportedException();
        }

        public void Listen(int port, string connectionKey)
        {
            throw new NotSupportedException();
        }

        public void ProcessRequests()
        {
            throw new NotSupportedException();
        }

        public async Task SendDataAsync(int? clientId, DeliveryMode deliveryMode, byte[] data)
        {
            if (deliveryMode != DeliveryMode.ReliableOrdered)
            {
                throw new NotSupportedException("Only ReliableOrdered is supported.");
            }

            if (clientId == null)
            {
                var sendTasks = new List<Task>();

                foreach (var peer in _peersById.Values)
                {
                    sendTasks.Add(peer.Socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None));
                }

                await Task.WhenAll(sendTasks);
            }
            else
            {
                if (_peersById.TryGetValue(clientId.Value, out var peer))
                {
                    await peer.Socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            _stopClcSrc.Cancel();
        }

        void receiveLoop(WebSocketPeer peer)
        {
            var memStream = new MemoryStream();
            var token = _stopClcSrc.Token;

            Task.Run(async () =>
            {
                try
                {
                    var buffer = new ArraySegment<byte>(new byte[2048]);
                    var totalBytesRead = 0;

                    while (!token.IsCancellationRequested)
                    {
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await peer.Socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
                            memStream.Write(buffer);
                            totalBytesRead += result.Count;
                        } while (!result.EndOfMessage);

                        if (totalBytesRead > 0 && _delegator != null)
                        {
                            await _delegator.OnDataReceivedAsync(peer, new ReadOnlyMemory<byte>(memStream.ToArray(), 0, totalBytesRead))
                                .ConfigureAwait(false);
                        }

                        totalBytesRead = 0;
                        memStream.Seek(0, SeekOrigin.Begin);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logging.LogError($"Websocket client error for {peer.RemoteEndpoint}. " + e.ToString());
                }

                peer.Dispose();
                _peersById.Remove(peer.PeerId);
                _delegator.OnPeerDisconnected(peer);
                memStream.Dispose();
            });
        }

        class WebSocketPeer : IPeerInfo, IDisposable
        {
            bool _disposed;
            readonly object _disLock = new object();

            public int PeerId { get; }

            public IPEndPoint RemoteEndpoint { get; }

            public object? UnderlyingConnection { get; }

            public WebSocket Socket { get; }

            public TaskCompletionSource<bool> DisposeInitiatedTcs { get; } = new TaskCompletionSource<bool>();

            public WebSocketPeer(int peerId, IPEndPoint remoteEndpoint, WebSocket socket)
            {
                PeerId = peerId;
                RemoteEndpoint = remoteEndpoint;
                UnderlyingConnection = socket;
                Socket = socket;
            }

            public void Dispose()
            {
                lock (_disLock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    Socket.Dispose();
                    DisposeInitiatedTcs.SetResult(true);
                    _disposed = true;
                }
            }
        }
    }
}
