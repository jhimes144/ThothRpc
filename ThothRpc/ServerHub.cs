using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Attributes;
using ThothRpc.Base;
using ThothRpc.Events;
using ThothRpc.Utility;

namespace ThothRpc
{
    /// <summary>
    /// Represents a server hub that listens for incoming connections and handles requests from clients.
    /// </summary>
    public class ServerHub : Hub, IServerDelegator, IDisposable
    {
        readonly IServer _server;

        /// <summary>
        /// Occurs when a peer connects to the server hub.
        /// </summary>
        public event EventHandler<PeerInfoEventArgs>? PeerConnected;

        /// <summary>
        /// Occurs when a peer disconnects from the server hub.
        /// </summary>
        public event EventHandler<PeerInfoEventArgs>? PeerDisconnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHub"/> class.
        /// </summary>
        /// <param name="server">The underlying server instance.</param>
        /// <param name="config">The configuration for the server hub.</param>
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

        /// <summary>
        /// Listens for incoming connections on the specified IPv4 nad IPv6 address and port.
        /// </summary>
        /// <param name="addressIPv4">The IPv4 address to listen on.</param>
        /// <param name="addressIPv6">The IPv6 address to listen on.</param>
        /// <param name="port">The port to listen on.</param>
        /// <param name="connectionKey">The connection key to use for authentication.</param>
        public void Listen(string addressIPv4, string addressIPv6, int port, string connectionKey)
        {
            _server.Listen(addressIPv4, addressIPv6, port, connectionKey);
        }

        /// <summary>
        /// Listens for incoming connections on the specified port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        /// <param name="connectionKey">The connection key to use for authentication.</param>
        public void Listen(int port, string connectionKey)
        {
            _server.Listen(port, connectionKey);
        }

        /// <summary>
        /// Registers an object instance with the server hub. This allows the instance to be called from clients.
        /// All public methods decorated with the <see cref="ThothMethodAttribute"/> will be used unless methodNames
        /// is specified, in which case method names provided will be used.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodNames">If specified, methods listed will be marked as accessible by peers.</param>
        /// <param name="targetName">(Optional) - Defaults to instance type name.</param>
        /// <exception cref="InvalidOperationException">Thrown if the target has already been registered under the specified name.</exception>
        public void Register(object instance, string? targetName = null, IEnumerable<string>? methodNames = null)
        {
            RegisterBase(instance, targetName, methodNames);
        }

        /// <summary>
        /// <para>
        /// Registers an object instance with the server hub. This allows the instance to be called from clients.
        /// All public methods decorated with the <see cref="ThothMethodAttribute"/> will be used unless methodNames
        /// is specified, in which case method names provided will be used.
        /// </para>
        /// <para>
        /// The difference between this method and <see cref="Register(object, string?, IEnumerable{string}?)"/>
        /// is that this method will register the target name under <typeparamref name="T"/> type full name as oppose to
        /// GetType() directly on the instance.
        /// </para>
        /// </summary>
        /// <param name="methodNames">If specified, methods listed will be marked as accessible by peers.</param>
        /// <typeparam name="T">The type to register target name under</typeparam>
        /// <param name="instance">he instance to use.</param>
        public void RegisterAs<T>(T instance, IEnumerable<string>? methodNames = null) where T : notnull
        {
            RegisterBase(instance, typeof(T).FullName, methodNames);
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

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the method call.</typeparam>
        /// <typeparam name="TTarget">The type of the target object on the client side.</typeparam>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="expression">An expression representing the method to be called on the client side.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the method call.</returns>
        public ValueTask<TResult> InvokeClientAsync<TResult, TTarget>(int clientId, Expression<Func<TTarget, TResult>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(clientId, expression, cancellationToken);
        }

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target object on the client side.</typeparam>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="expression">An expression representing the method to be called on the client side.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public ValueTask InvokeClientAsync<TTarget>(int clientId, Expression<Action<TTarget>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(clientId, expression, cancellationToken);
        }

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the method call.</typeparam>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the method call.</returns>
        public ValueTask<TResult> InvokeClientAsync<TResult>(int clientId, string targetClass, string method, params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(clientId, targetClass, method, default, parameters);
        }

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async ValueTask InvokeClientAsync(int clientId, string targetClass, string method, params object[] parameters)
        {
            await InvokeRemoteAsync(clientId, targetClass, method, null, default, parameters);
        }

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the method call.</typeparam>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the method call.</returns>
        public ValueTask<TResult> InvokeClientAsync<TResult>(int clientId, string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(clientId, targetClass, method, cancellationToken, parameters);
        }

        /// <summary>
        /// Invokes a method on a client asynchronously.
        /// </summary>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async ValueTask InvokeClientAsync(int clientId, string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            await InvokeRemoteAsync(clientId, targetClass, method, null, cancellationToken, parameters);
        }

        /// <summary>
        /// Invokes a method on a client, ignoring the result and any exceptions that may occur.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode to use when invoking the method.</param>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        public void InvokeForgetClient(DeliveryMode deliveryMode, int clientId, string targetClass,
            string method, params object?[] parameters)
        {
            InvokeForgetRemote(deliveryMode, clientId, targetClass, method, parameters);
        }

        /// <summary>
        /// Invokes a method on all clients, ignoring the result and any exceptions that may occur.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode to use when invoking the method.</param>
        /// <param name="targetClass">The target class on the client side to invoke the method on.</param>
        /// <param name="method">The method to be called on the client side.</param>
        /// <param name="parameters">The parameters to pass to the method on the client side.</param>
        public void InvokeForgetAllClients(DeliveryMode deliveryMode, string targetClass,
            string method, params object?[] parameters)
        {
            InvokeForgetRemote(deliveryMode, null, targetClass, method, parameters);
        }

        /// <summary>
        /// Invokes a method on a client, ignoring the result and any exceptions that may occur.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode to use when invoking the method.</param>
        /// <param name="clientId">The id of the client to invoke the method on.</param>
        /// <param name="expression">An expression that represents the method to be called on the client side.</param>
        public void InvokeForgetClient<TTarget>(DeliveryMode deliveryMode, int clientId, Expression<Action<TTarget>> expression)
        {
            InvokeForgetRemote(deliveryMode, clientId, expression);
        }

        /// <summary>
        /// Invokes a method on all clients, ignoring the result and any exceptions that may occur.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode to use when invoking the method.</param>
        /// <param name="expression">An expression that represents the method to be called on the client side.</param>
        public void InvokeForgetAllClients<TTarget>(DeliveryMode deliveryMode, Expression<Action<TTarget>> expression)
        {
            InvokeForgetRemote(deliveryMode, null, expression);
        }

        protected override void SendData(DeliveryMode deliveryMode, int? peerId, byte[] data)
        {
            _server.SendData(peerId, deliveryMode, data);
        }

        /// <summary>
        /// Releases all resources used by <see cref="ServerHub"/>. Stops any listening and closes network sockets.
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
