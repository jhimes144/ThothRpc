using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Xml.XPath;
using ThothRpc.Exceptions;
using ThothRpc.Models;
using ThothRpc.Models.Dto;
using ThothRpc.Optimizer;
using ThothRpc.Utility;

// TODO: Move these to project file.
[assembly: InternalsVisibleTo("ThothRpc.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace ThothRpc.Base
{
    /// <summary>
    /// Base class for hubs.
    /// </summary>
    public abstract class Hub
    {
        [ThreadStatic]
        static IPeerInfo? _currentPeer;

        uint _currentCallId;

        readonly IPeerInfo _localPeerInfo = new PeerInfo
        {
            PeerId = -1,
            RemoteEndpoint = null,
            UnderlyingConnection = null
        };

        readonly ObjectPool<AwaitingCall> _awaitingCallPool = 
            new ObjectPool<AwaitingCall>(() => new AwaitingCall(), a =>
            {
                a.TaskCompletionSource.Reset();
                a.ReturnType = null;
                a.SyncException = null;
                a.SyncReturnData = null;

                a.ResetEvent.Dispose();
                a.ResetEvent = new ManualResetEventSlim(false);

#if NET6_0_OR_GREATER
                if (!a.TimeoutCancelSrc.TryReset())
                {
                    a.TimeoutCancelSrc.Dispose();
                    a.TimeoutCancelSrc = new CancellationTokenSource();
                }
#else
                a.TimeoutCancelSrc.Dispose();
                a.TimeoutCancelSrc = new CancellationTokenSource();
#endif
            }, 30);

        readonly bool _swallowExceptions;
        readonly bool _genericErrorMessages;
        readonly bool _isClient;

        readonly Dictionary<string, TargetRegistration> _targetsByName
            = new Dictionary<string, TargetRegistration>();

        readonly ReaderWriterLockSlim _targetsLock = new ReaderWriterLockSlim();
        readonly ReaderWriterLockSlim _localHubLock = new ReaderWriterLockSlim();
        Hub? _localHub;

        readonly Dictionary<uint, AwaitingCall> _awaitingCallByCallId
            = new Dictionary<uint, AwaitingCall>();

        readonly object _callStateLock = new object();

        readonly SerializeObject _objectSerializer;
        readonly DeserializeObject _objectDeserializer;
        readonly Func<byte[], byte[]>? _dataIngressTransformer;
        readonly Func<byte[], byte[]>? _dataEgressTransformer;
        readonly PacketAnalyzer _packetAnalyzer;
        readonly TimeSpan _requestTimeout;

        /// <summary>
        /// The current configuration used by the hub. This object is immutable.
        /// </summary>
        public IHubConfiguration Config { get; }

        volatile bool _disposed;

        protected Hub(bool isClient, IHubConfiguration config) 
        {
            Guard.AgainstNull(nameof(config), config);
            Config = config;

            _swallowExceptions = config.SwallowExceptions;
            _genericErrorMessages = config.GenericErrorMessages;
            _isClient = isClient;

            _objectSerializer = config.ObjectSerializer;
            _objectDeserializer = config.ObjectDeserializer;
            _dataIngressTransformer = config.DataIngressTransformer;
            _dataEgressTransformer = config.DataEgressTransformer;
            _requestTimeout = config.RequestTimeout;

            if (_objectSerializer == null)
            {
                throw new Exception("ObjectSerializer must be configured.");
            }

            if (_objectDeserializer == null)
            {
                throw new Exception("ObjectDeserializer must be configured.");
            }

            _packetAnalyzer = new PacketAnalyzer(
                ThothOptimizer.Instance,
                Pools.MethodCallDtoPool.Rent,
                Pools.MethodResponseDtoPool.Rent);
        }

        /// <summary>
        /// Attaches a client hub to a server hub so that calls made between the two occur in process rather
        /// than over the network. When <see cref="IPeerInfo"/> is passed for traffic from attached hub, it will have a peer id of -1.
        /// Direct calls made to the local client from the server should use a peer id of -1.
        /// NOTE: Hubs can only ever be locally attached to one other hub.
        /// </summary>
        /// <param name="server">Server to attach</param>
        /// <param name="client">Client to attach</param>
        [Obsolete]
        internal static void AttachLocalHubs(ServerHub server, ClientHub client)
        {
            server.checkThrowDisposed();
            client.checkThrowDisposed();

            server._localHubLock.EnterWriteLock();
            client._localHubLock.EnterWriteLock();

            server._localHub = client;
            client._localHub = server;

            server._localHubLock.ExitWriteLock();
            client._localHubLock.ExitWriteLock();
        }

        protected void RegisterBase(object instance, string? targetName = null, IEnumerable<string>? methodNames = null)
        {
            checkThrowDisposed();
            Guard.AgainstNull(nameof(instance), instance);
            targetName ??= instance.GetType().FullName;
            Guard.AgainstNullOrWhiteSpaceString(targetName, nameof(targetName));

            _targetsLock.EnterWriteLock();

            try
            {
                if (_targetsByName.ContainsKey(targetName!))
                {
                    throw new InvalidOperationException($"A target with name ${targetName} has already been registered.");
                }

                var registration = new TargetRegistration
                {
                    Instance = instance
                };

                if (methodNames == null)
                {
                    registration.Methods = ReflectionHelper.GetThothMethods(instance.GetType())
                        .ToList();
                }
                else
                {
                    var methodNamesL = methodNames.ToList();
                    var methods = instance.GetType().GetMethods();

                    foreach (var methodName in methodNamesL)
                    {
                        var method = methods.FirstOrDefault(m => m.Name == methodName);

                        if (method == null)
                        {
                            throw new InvalidOperationException
                                ($"Cannot find public method {methodName} in ${instance.GetType().FullName}");
                        }

                        registration.Methods.Add(method);
                    }
                }

                if (registration.Methods.Select(m => m.Name).Distinct().Count()
                    != registration.Methods.Select(m => m.Name).Count())
                {
                    throw new NotSupportedException("Overloaded methods are not supported.");
                }

                _targetsByName.Add(targetName!, registration);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _targetsLock.ExitWriteLock();
            }
        }

        protected void UnregisterBase(string targetName) 
        {
            checkThrowDisposed();
            Guard.AgainstNullOrWhiteSpaceString(targetName, nameof(targetName));

            _targetsLock.EnterWriteLock();

            if (!_targetsByName.ContainsKey(targetName))
            {
                _targetsLock.ExitWriteLock();
                throw new InvalidOperationException($"Target ${targetName} was never registered.");
            }

            _targetsByName.Remove(targetName);
            _targetsLock.ExitWriteLock();
        }

        protected ValueTask<TResult> InvokeRemoteAsync<TResult, TTarget>(int? clientId,
            Expression<Func<TTarget, TResult>> expression, CancellationToken cancellationToken = default)
        {
            checkThrowDisposed();
            (string MethodName, object?[] Arguments) = ReflectionHelper.EvaluateMethodCall(expression);

            return InvokeRemoteAsync<TResult>(clientId, typeof(TTarget).FullName!,
                MethodName, cancellationToken, Arguments);
        }

        protected async ValueTask InvokeRemoteAsync<TTarget>(int? clientId,
            Expression<Action<TTarget>> expression, CancellationToken cancellationToken = default)
        {
            checkThrowDisposed();
            (string MethodName, object?[] Arguments) = ReflectionHelper.EvaluateMethodCall(expression);

            await InvokeRemoteAsync<object>(clientId, typeof(TTarget).FullName!,
                MethodName, cancellationToken, Arguments).ConfigureAwait(false);
        }

        protected async ValueTask<TResult> InvokeRemoteAsync<TResult>
            (int? clientId, string targetClass, string method,
            CancellationToken cancellationToken, params object?[] parameters)
        {
            checkThrowDisposed();
            var result = await InvokeRemoteAsync(clientId, targetClass, method,
                typeof(TResult), cancellationToken, parameters).ConfigureAwait(false);

            if (result == default)
            {
                return default!;
            }
            else
            {
                try
                {
                    return (TResult)result;
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCallException($"Return type mismatch. Cannot convert " +
                        $"{result.GetType().Name} to {typeof(TResult).Name}.", e);
                }
            }
        }

        protected TResult InvokeRemote<TResult, TTarget>(int? clientId,
            Expression<Func<TTarget, TResult>> expression)
        {
            checkThrowDisposed();
            (string MethodName, object?[] Arguments) = ReflectionHelper.EvaluateMethodCall(expression);

            return InvokeRemote<TResult>(clientId, typeof(TTarget).FullName!,
                MethodName, Arguments);
        }

        protected void InvokeRemote<TTarget>(int? clientId,
            Expression<Action<TTarget>> expression)
        {
            checkThrowDisposed();

            (string MethodName, object?[] Arguments) = ReflectionHelper.EvaluateMethodCall(expression);
            InvokeRemote<object>(clientId, typeof(TTarget).FullName!, MethodName, Arguments);
        }

        protected TResult InvokeRemote<TResult>
            (int? clientId, string targetClass, string method, params object?[] parameters)
        {
            checkThrowDisposed();

            var result = InvokeRemote(clientId, targetClass,
                method, typeof(TResult), parameters);

            if (result == default)
            {
                return default!;
            }
            else
            {
                try
                {
                    return (TResult)result;
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCallException($"Return type mismatch. Cannot convert " +
                        $"{result.GetType().Name} to {typeof(TResult).Name}.", e);
                }
            }
        }

        protected object? InvokeRemote(int? clientId,
            string targetClass,
            string method,
            Type? returnType,
            params object?[] parameters)
        {
            checkThrowDisposed();
            Guard.AgainstNullOrWhiteSpaceString(targetClass, nameof(targetClass));
            Guard.AgainstNullOrWhiteSpaceString(method, nameof(method));

            var awaitingCall = _awaitingCallPool.Rent();
            awaitingCall.IsSynchronous = true;
            awaitingCall.ReturnType = returnType;
            var callId = addAwaitingCall(awaitingCall);

            var payload = Pools.MethodCallDtoPool.Rent();
            payload.CallId = callId;
            payload.Method = method;
            payload.ClassTarget = targetClass;
            fillParameters(parameters, payload.ArgumentsData);

            try
            {
                sendDto(DeliveryMode.ReliableOrdered, clientId, payload);
                awaitingCall.ResetEvent.Wait(_requestTimeout);

                if (awaitingCall.SyncException != null)
                {
                    throw awaitingCall.SyncException;
                }
                else
                {
                    return awaitingCall.SyncReturnData;
                }
            }
            catch (OperationCanceledException e)
            {
                throw new TimeoutException("Operation has timed-out", e);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                lock (_callStateLock)
                {
                    _awaitingCallByCallId.Remove(callId);
                }

                _awaitingCallPool.Recycle(awaitingCall);
                Pools.MethodCallDtoPool.Recycle(payload);
            }
        }

        uint addAwaitingCall(AwaitingCall awaitingCall)
        {
            uint callId = 0;

            lock (_callStateLock)
            {
                // yes we are aware you can just ++ the whole time and it goes back to 0
                // this is here for clear intent.

                if (_currentCallId == uint.MaxValue)
                {
                    _currentCallId = 0;
                }
                else
                {
                    _currentCallId++;
                }

                callId = _currentCallId;
                _awaitingCallByCallId.Add(callId, awaitingCall);
            }

            return callId;
        }

        protected async ValueTask<object?> InvokeRemoteAsync(
            int? clientId,
            string targetClass,
            string method,
            Type? returnType,
            CancellationToken cancellationToken,
            params object?[] parameters)
        {
            checkThrowDisposed();
            Guard.AgainstNullOrWhiteSpaceString(targetClass, nameof(targetClass));
            Guard.AgainstNullOrWhiteSpaceString(method, nameof(method));

            var awaitingCall = _awaitingCallPool.Rent();
            awaitingCall.IsSynchronous = false;
            awaitingCall.ReturnType = returnType;
            awaitingCall.TimeoutCancelSrc.CancelAfter(_requestTimeout);
            var tcs = awaitingCall.TaskCompletionSource;
            var callId = addAwaitingCall(awaitingCall);

            void cancelCall(uint callId, Exception exception)
            {
                lock (_callStateLock)
                {
                    if (_awaitingCallByCallId.TryGetValue(callId, out var call))
                    {
                        tcs.SetException(exception);
                        _awaitingCallByCallId.Remove(callId);
                    }
                }
            }

            // fyi closures here, memory allocation.
            cancellationToken.Register(() => cancelCall(callId, new TaskCanceledException()));
            awaitingCall.TimeoutCancelSrc.Token.Register(() => cancelCall(callId, new TimeoutException()));

            var payload = Pools.MethodCallDtoPool.Rent();
            payload.CallId = callId;
            payload.Method = method;
            payload.ClassTarget = targetClass;
            fillParameters(parameters, payload.ArgumentsData);

            var task = new ValueTask<object?>(tcs, tcs.Version);

            try
            {
                await sendDtoAsync(DeliveryMode.ReliableOrdered, clientId, payload).ConfigureAwait(false);
                var result = await task.ConfigureAwait(false);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                lock (_callStateLock)
                {
                    _awaitingCallByCallId.Remove(callId);
                }

                _awaitingCallPool.Recycle(awaitingCall);
                Pools.MethodCallDtoPool.Recycle(payload);
            }
        }

        protected void InvokeForgetRemote<TTarget>(DeliveryMode deliveryMode, int? clientId,
            Expression<Action<TTarget>> expression)
        {
            checkThrowDisposed();
            (string MethodName, object?[] Arguments) = ReflectionHelper.EvaluateMethodCall(expression);

            InvokeForgetRemote(deliveryMode, clientId, typeof(TTarget).FullName!,
                MethodName, Arguments);
        }

        protected async void InvokeForgetRemote(DeliveryMode deliveryMode, int? clientId, string targetClass,
            string method, params object?[] parameters)
        {
            checkThrowDisposed();
            Guard.AgainstNullOrWhiteSpaceString(targetClass, nameof(targetClass));
            Guard.AgainstNullOrWhiteSpaceString(method, nameof(method));

            var payload = Pools.MethodCallDtoPool.Rent();
            payload.CallId = null;
            payload.Method = method;
            payload.ClassTarget = targetClass;
            fillParameters(parameters, payload.ArgumentsData);

            try
            {
                await sendDtoAsync(deliveryMode, clientId, payload).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.LogError($"Failed to send message for fire and forget -" +
                    $" Target {payload.ClassTarget} -> {payload.Method} | {e}");
            }
            finally
            {
                Pools.Recycle(payload);
            }
        }

        void fillParameters(object?[] parameters, List<ReadOnlyMemory<byte>> list)
        {
            list.Clear();

            foreach (var parameter in parameters)
            {
                list.Add(_objectSerializer(parameter));
            }
        }

        public virtual void Dispose()
        {
            checkThrowDisposed();
            _disposed = true;
            _targetsLock.Dispose();
            _localHubLock.Dispose();
        }

        /// <summary>
        /// Returns the current peer who called the method currently executing. 
        /// This should only be called within a method decorated with <see cref="Attributes.ThothMethodAttribute"/>.
        /// Its important to note that if a Thoth method is called locally (as in directly invoked not through ThothRpc), that
        /// the peer returned will likely be peer info for a different call, and should not be used.
        /// </summary>
        /// <returns></returns>
        public IPeerInfo? GetCurrentPeer()
        {
            checkThrowDisposed();
            return _currentPeer;
        }

        async ValueTask processMethodCallAsync(IPeerInfo? peer, MethodCallDto dto)
        {
            MethodResponseDto? responseDto = null;

            if (dto.CallId.HasValue)
            {
                responseDto = Pools.MethodResponseDtoPool.Rent();
                responseDto.CallId = dto.CallId.Value;
            }

            Exception? ex = null;
            object? result = null;
            TargetRegistration? target = null;

            if (dto.ClassTarget != null)
            {
                _targetsLock.EnterReadLock();
                _targetsByName.TryGetValue(dto.ClassTarget, out target);
                _targetsLock.ExitReadLock();
            }

            if (target != null)
            {
                try
                {
                    _currentPeer = peer;

                    result = await MethodInvoker.InvokeMethodAsync(target, dto.Method,
                        dto.ArgumentsData, _objectDeserializer).ConfigureAwait(false);

                }
                catch (InvalidCallException e)
                {
                    ex = e;
                }
                catch (CallFailedException e)
                {
                    ex = e;
                }
                catch (Exception e)
                {
                    ex = new CallFailedException("Unknown exception occurred. " + e.Message);
                }
            }
            else
            {
                ex = new InvalidCallException(!string.IsNullOrWhiteSpace(dto.ClassTarget) 
                    ? $"Failed to find target {dto.ClassTarget}" : 
                    "Method call sent with no target.");
            }

            if (responseDto != null)
            {
                if (!_swallowExceptions)
                {
                    responseDto.Exception = ex == null ? null : ExceptionContainer.Pack(ex, _genericErrorMessages);
                }

                if (result != null)
                {
                    responseDto.ResultData = _objectSerializer(result);
                }

                // this seems to be a bug in .net, the following sets ResultData to an empty memory span when null
                //responseDto.ResultData = result == null ? null : _objectSerializer(result);

                var doIt = false;

                if (doIt)
                {
                    responseDto.ResultData = null;
                }

                await sendDtoAsync(DeliveryMode.ReliableOrdered, peer?.PeerId, responseDto).ConfigureAwait(false);
                Pools.MethodResponseDtoPool.Recycle(responseDto);
            }

            if (ex != null)
            {
                Logging.LogError("Failure executing method call request. " + ex.ToString());
            }
        }

        void processMethodResponse(MethodResponseDto dto)
        {
            lock (_callStateLock)
            {
                if (_awaitingCallByCallId.TryGetValue(dto.CallId, out var awaitingCall))
                {
                    if (awaitingCall.IsSynchronous)
                    {
                        if (dto.Exception != null)
                        {
                            awaitingCall.SyncException = dto.Exception.Unpack();
                        }
                        else if (dto.ResultData == null)
                        {
                            awaitingCall.SyncReturnData = null;
                        }
                        else
                        {
                            var des = _objectDeserializer(awaitingCall.ReturnType ?? typeof(object), dto.ResultData.Value);
                            awaitingCall.SyncReturnData = des;
                        }

                        awaitingCall.ResetEvent.Set();
                    }
                    else
                    {
                        if (dto.Exception != null)
                        {
                            awaitingCall.TaskCompletionSource
                                .SetException(dto.Exception.Unpack());
                        }
                        else if (dto.ResultData == null)
                        {
                            awaitingCall.TaskCompletionSource.SetResult(null);
                        }
                        else
                        {
                            var des = _objectDeserializer(awaitingCall.ReturnType ?? typeof(object), dto.ResultData.Value);
                            awaitingCall.TaskCompletionSource.SetResult(des);
                        }
                    }
                }
                else
                {
                    Logging.LogWarn("Received unsolicited method response from " +
                        $"server payload id {dto.CallId}. Was the method cancelled?");
                }
            }
        }

        void sendDto(DeliveryMode deliveryMode, int? clientId, IThothDto dto)
        {
            var data = _packetAnalyzer.SerializePacket(dto);
            SendData(deliveryMode, clientId, data);
        }

        async ValueTask sendDtoAsync(DeliveryMode deliveryMode, int? clientId, IThothDto dto)
        {
            var skipNetworkSend = false;

            if (!clientId.HasValue || clientId == -1)
            {
                _localHubLock.EnterReadLock();
                var localHub = _localHub;
                _localHubLock.ExitReadLock();

                if (localHub != null)
                {
                    await localHub.onObjectReceivedAsync(_localPeerInfo, dto).ConfigureAwait(false);
                    skipNetworkSend = clientId == -1 || _isClient;
                }
            }

            if (!skipNetworkSend)
            {
                var data = _packetAnalyzer.SerializePacket(dto);
                SendData(deliveryMode, clientId, data);
            }
        }

        async ValueTask onObjectReceivedAsync(IPeerInfo? peerInfo, IThothDto obj)
        {
            switch (obj)
            {
                case MethodCallDto methodCall:
                    await processMethodCallAsync(peerInfo, methodCall).ConfigureAwait(false);
                    break;
                case MethodResponseDto methodResponse:
                    processMethodResponse(methodResponse);
                    break;
                default:
                    break;
            }
        }

        protected async ValueTask OnDataRecievedAsync(IPeerInfo? peerInfo, byte[] data)
        {
            IThothDto? dto = null;

            try
            {
                data = _dataIngressTransformer?.Invoke(data) ?? data;
                dto = _packetAnalyzer.DeserializePacket(data);

                await onObjectReceivedAsync(peerInfo, dto).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logging.LogError($"Failed to process incoming message. {e}");
            }
            finally
            {
                if (dto != null)
                {
                    Pools.Recycle(dto);
                }
            }
        }

        void checkThrowDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        protected abstract void SendData(DeliveryMode deliveryMode, int? clientId, byte[] data);

        private class AwaitingCall : IDisposable
        {
            public bool IsSynchronous { get; set; }

            public ManualResetEventSlim ResetEvent { get; set; } 
                = new ManualResetEventSlim(false);

            public object? SyncReturnData { get; set; }

            public Exception? SyncException { get; set; }

            public ManualResetValueTaskSource<object?> TaskCompletionSource { get; }
                = new ManualResetValueTaskSource<object?>()
                {
                    RunContinuationsAsynchronously = true
                };

            public Type? ReturnType { get; set; }

            public CancellationTokenSource TimeoutCancelSrc { get; set; } 
                = new CancellationTokenSource();

            public void Dispose()
            {
                ResetEvent.Dispose();
                TimeoutCancelSrc.Dispose();
            }
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
