using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc.Websockets
{
    public class ThothWebsocketClient : IClient
    {
        CancellationTokenSource _receiveClcSrc = new CancellationTokenSource();
        Task? _receiveTask;
        Task? _healTask;
        ClientWebSocket? _socket;
        TimeSpan _connectTimeout;
        IClientDelegator? _delegator;
        string? _address;
        int _port;
        bool _disposed;

        public ConnectionState ConnectionState { get; private set; }

        public event EventHandler ConnectionStateChange;

        public async Task ConnectAsync(string address, int port, string connectionKey)
        {
            _address = address;
            _port = port;

            await insureHealingAsync();
        }

        public async Task DisconnectAsync()
        {
            if (_socket != null)
            {
                _receiveClcSrc.Cancel();

                if (_receiveTask != null)
                {
                    await _receiveTask.ConfigureAwait(false);
                }

                _receiveClcSrc.Dispose();
                _receiveClcSrc = new CancellationTokenSource();
                setConnectionState(ConnectionState.Disconnected);
                _socket.Dispose();
                _delegator?.OnDisconnected();
                _socket = null;
            }
        }

        async Task insureHealingAsync()
        {
            insureHealing();
            await _healTask.ConfigureAwait(false);
        }

        void insureHealing()
        {
            if (_healTask == null || _healTask.IsCompleted)
            {
                _healTask = healAsync();
            }
        }

        async Task healAsync()
        {
            Exception? ex = null;
            await DisconnectAsync().ConfigureAwait(false);

            try
            {
                setConnectionState(ConnectionState.Connecting);
                var clcSrc = new CancellationTokenSource();
                clcSrc.CancelAfter(_connectTimeout);

                _socket = new ClientWebSocket();

                await _socket.ConnectAsync(new Uri($"{_address}:{_port}"), clcSrc.Token)
                    .ConfigureAwait(false);

                clcSrc.Dispose();

                _receiveTask = receiveLoopAsync(_receiveClcSrc.Token);
                _delegator?.OnConnected();

                setConnectionState(ConnectionState.Connected);
                Logging.LogInfo("Websocket connection made.");
            }
            catch (OperationCanceledException e)
            {
                ex = e;
                Logging.LogError("Websocket connection timed-out.");
            }
            catch (Exception e)
            {
                ex = e;
                Logging.LogError("Websocket connection error. " + e.ToString());
            }

            if (ex != null)
            {
                Logging.LogInfo("Pausing for eventual retry.");
                await Task.Delay(TimeSpan.FromSeconds(4));
                Logging.LogInfo($"Attempting websocket reconnection ...");
                await healAsync().ConfigureAwait(false);
            }
        }

        async Task receiveLoopAsync(CancellationToken clcToken)
        {
            using var memStream = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[2048]);
            var totalBytesRead = 0;

            try
            {
                while (!clcToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _socket.ReceiveAsync(buffer, clcToken).ConfigureAwait(false);
                        memStream.Write(buffer);
                        totalBytesRead += result.Count;
                    } while (!result.EndOfMessage);

                    if (totalBytesRead > 0 && _delegator != null)
                    {
                        await _delegator.OnDataReceivedAsync(new ReadOnlyMemory<byte>(memStream.ToArray(), 0, totalBytesRead))
                            .ConfigureAwait(false);
                    }

                    totalBytesRead = 0;
                    memStream.Seek(0, SeekOrigin.Begin);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        throw new Exception("Websocket connection closed.");
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                Logging.LogError($"Websocket error during receiving. {e}");

                if (!clcToken.IsCancellationRequested)
                {
                    insureHealing();
                }
            }
        }

        void setConnectionState(ConnectionState state)
        {
            ConnectionState = state;
            ConnectionStateChange?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _disposed = true;
            _ = DisconnectAsync().ConfigureAwait(false);
        }

        public void Init(IClientDelegator delegator, TimeSpan connectingTimeout, RequestHandlingStrategy requestHandling, TimeSpan disconnectTimeout)
        {
            if (requestHandling != RequestHandlingStrategy.SingleThreaded)
            {
                throw new NotSupportedException("Only SingleThreaded strategy is supported.");
            }

            _connectTimeout = connectingTimeout;
            _delegator = delegator;
        }

        public void ProcessRequests()
        {
            throw new NotSupportedException();
        }

        public async Task SendDataAsync(DeliveryMode deliveryMode, byte[] data)
        {
            if (_socket == null)
            {
                await insureHealingAsync().ConfigureAwait(false);
            }

            try
            {
                await _socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.LogError($"Websocket error during sending. {e}");
                await insureHealingAsync().ConfigureAwait(false);
                await SendDataAsync(deliveryMode, data).ConfigureAwait(false);
            }
        }

        public void Disconnect()
        {
            _ = DisconnectAsync();
        }
    }
}