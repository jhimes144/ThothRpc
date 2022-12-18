using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Base;
using ThothRpc.Events;
using ThothRpc.Utility;

namespace ThothRpc
{
    public class ServerHub : Hub, IServerDelegator, IDisposable
    {
        readonly IServer _server;

        public event EventHandler<PeerInfoEventArgs>? PeerConnected;
        public event EventHandler<PeerInfoEventArgs>? PeerDisconnected;

        public ServerHub(IServer server, IHubConfiguration config)
            : base(false, config)
        {
            _server = server;

            _server.Init(
                delegator: this,
                multiThreaded: config.RequestHandlingStrategy == RequestHandlingStrategy.MultiThreaded
            );
        }

        /// <summary>
        /// Process all pending requests. If application has a main loop (such as a game), this should be called there.
        /// This will process all pending requests on the current thread.
        /// Note that requestHandlingStrategy should be Manual if this method is going to be used.
        /// </summary>
        public void ProcessRequests()
        {
            _server.ProcessRequests();
        }

        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey)
        {
            _server.Listen(addressIPv4, addressIPv6, port, connectionKey);
        }

        public void Listen(int port, string connectionKey)
        {
            _server.Listen(port, connectionKey);
        }

        /// <summary>
        /// Registers an object instance with the server hub. This allows the instance to be called from the client.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="targetName">(Optional) - Defaults to instance type name.</param>
        /// <exception cref="InvalidOperationException">Thrown if the target has already been registered under the specified name.</exception>
        public void Register(object instance, string? targetName = null)
        {
            RegisterBase(instance, targetName);
        }

        /// <summary>
        /// Unregisters an object instance previously registered with this Hub.
        /// </summary>
        /// <param name="targetName">Target name of instance.</param>
        /// <exception cref="InvalidOperationException">Thrown if instance cannot be found by targetName.</exception>
        public void Unregister(string targetName)
        {
            UnregisterBase(targetName);
        }

        public void Unregister(object instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            UnregisterBase(instance.GetType().FullName);
        }

        public ValueTask<TResult> InvokeClientAsync<TResult, TTarget>(int clientId, Expression<Func<TTarget, TResult>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(clientId, expression, cancellationToken);
        }

        public ValueTask InvokeClientAsync<TTarget>(int clientId, Expression<Action<TTarget>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(clientId, expression, cancellationToken);
        }

        public ValueTask<TResult> InvokeClientAsync<TResult>(int clientId, string targetClass, string method, params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(clientId, targetClass, method, default, parameters);
        }

        public async ValueTask InvokeClientAsync(int clientId, string targetClass, string method, params object[] parameters)
        {
            await InvokeRemoteAsync(clientId, targetClass, method, null, default, parameters);
        }

        public ValueTask<TResult> InvokeClientAsync<TResult>(int clientId, string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(clientId, targetClass, method, cancellationToken, parameters);
        }

        public async ValueTask InvokeClientAsync(int clientId, string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            await InvokeRemoteAsync(clientId, targetClass, method, null, cancellationToken, parameters);
        }

        public void InvokeForgetClient(DeliveryMode deliveryMode, int clientId, string targetClass,
            string method, params object?[] parameters)
        {
            InvokeForgetRemote(deliveryMode, clientId, targetClass, method, parameters);
        }

        public void InvokeForgetAllClients(DeliveryMode deliveryMode, string targetClass,
            string method, params object?[] parameters)
        {
            InvokeForgetRemote(deliveryMode, null, targetClass, method, parameters);
        }

        public void InvokeForgetClient<TTarget>(DeliveryMode deliveryMode, int clientId, Expression<Action<TTarget>> expression)
        {
            InvokeForgetRemote(deliveryMode, clientId, expression);
        }

        public void InvokeForgetAllClients<TTarget>(DeliveryMode deliveryMode, Expression<Action<TTarget>> expression)
        {
            InvokeForgetRemote(deliveryMode, null, expression);
        }

        protected override void SendData(DeliveryMode deliveryMode, int? peerId, byte[] data)
        {
            _server.SendData(peerId, deliveryMode, data);
        }

        /// <summary>
        /// Releases all resources used by <see cref="ServerHub"/>
        /// </summary>
        public override void Dispose()
        {
            _server.Dispose();
            base.Dispose();
        }

        void IServerDelegator.OnPeerConnected(IPeerInfo peer)
        {
            PeerConnected?.Invoke(this, new PeerInfoEventArgs(peer));
        }

        void IServerDelegator.OnPeerDisconnected(IPeerInfo peer)
        {
            PeerDisconnected?.Invoke(this, new PeerInfoEventArgs(peer));
        }

        ValueTask IServerDelegator.OnDataReceivedAsync(IPeerInfo peer, byte[] data)
        {
            return OnDataRecievedAsync(peer, data);
        }
    }
}
