using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ThothRpc.Exceptions;
using ThothRpc.Base;
using ThothRpc.Models;
using ThothRpc.Models.Dto;
using ThothRpc.Utility;

namespace ThothRpc
{
    /// <summary>
    /// Handles all client related operations.
    /// </summary>
    public class ClientHub : Hub, IClientDelegator, IDisposable
    {
        readonly IClient _client;

        public event EventHandler? Connected;
        public event EventHandler? Disconnected;

        public ClientHub(IClient client, IHubConfiguration config) 
            : base(true, config)
        {
            Guard.AgainstNull(nameof(client), client);
            _client = client;

            _client.Init(
                delegator: this,
                multiThreaded: config.RequestHandlingStrategy == RequestHandlingStrategy.MultiThreaded
            );
        }

        public ClientHub(ServerHub localServer, IHubConfiguration config)
            : base(true, config)
        {
            Guard.AgainstNull(nameof(localServer), localServer);
            AttachLocalHubs(localServer, this);
        }

        /// <summary>
        /// Process all pending requests. If application has a main loop (such as a game), this should be called there.
        /// This will process all pending requests on the current thread.
        /// Note that requestHandlingStrategy should be Manual if this method is going to be used.
        /// </summary>
        public void ProcessRequests()
        {
            if (_client == null)
            {
                throw new InvalidOperationException
                    ("This method is unallowed when connected to a local server.");
            }

            _client.ProcessRequests();
        }

        public ValueTask ConnectAsync(string address, int port, string connectionKey)
        {
            if (_client == null)
            {
                throw new InvalidOperationException
                    ("Client cannot be connected to a server when one is already attached locally.");
            }

            return _client.ConnectAsync(address, port, connectionKey);
        }

        /// <summary>
        /// Registers an object instance with the client hub. This allows the instance to be called from the server.
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
            UnregisterBase(instance.GetType().FullName!);
        }

        /// <summary>
        /// Invokes a server method asynchronously and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <typeparam name="TTarget">The type of the server class.</typeparam>
        /// <param name="expression">The expression that represents the method to be called on the server.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The result of the task is the return value of the method.</returns>
        public ValueTask<TResult> InvokeServerAsync<TTarget, TResult>(Expression<Func<TTarget, TResult>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(null, expression, cancellationToken);
        }

        /// <summary>
        /// Invokes a server method asynchronously.
        /// </summary>
        /// <typeparam name="TTarget">The type of the server class.</typeparam>
        /// <param name="expression">The expression that represents the method to be called on the server.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public ValueTask InvokeServerAsync<TTarget>(Expression<Action<TTarget>> expression,
            CancellationToken cancellationToken = default)
        {
            return InvokeRemoteAsync(null, expression, cancellationToken);
        }

        /// <summary>
        /// Invokes a server method asynchronously and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        /// <returns>A task that represents the asynchronous operation. The result of the task is the return value of the method.</returns>
        public ValueTask<TResult> InvokeServerAsync<TResult>(string targetClass, string method, params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(null, targetClass, method, default, parameters);
        }

        /// <summary>
        /// Invokes a server method asynchronously.
        /// </summary>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async ValueTask InvokeServerAsync(string targetClass, string method, params object[] parameters)
        {
            await InvokeRemoteAsync(null, targetClass, method, null, default, parameters);
        }

        /// <summary>
        /// Invokes a server method asynchronously and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the operation.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        /// <returns>A task that represents the asynchronous operation. The result of the task is the return value of the method.</returns>
        public ValueTask<TResult> InvokeServerAsync<TResult>(string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            return InvokeRemoteAsync<TResult>(null, targetClass, method, cancellationToken, parameters);
        }

        /// <summary>
        /// Invokes a server method asynchronously.
        /// </summary>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="cancellationToken">The token that can be used to cancel the operation.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async ValueTask InvokeServerAsync(string targetClass, string method, CancellationToken cancellationToken,
            params object[] parameters)
        {
            await InvokeRemoteAsync(null, targetClass, method, null, cancellationToken, parameters);
        }

        /// <summary>
        /// Invokes a server method without waiting for a response.
        /// </summary>
        /// <param name="deliveryMode">The delivery mode for the method call.</param>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        public void InvokeForgetServer(DeliveryMode deliveryMode, string targetClass,
            string method, params object?[] parameters)
        {
            InvokeForgetRemote(deliveryMode, null, targetClass, method, parameters);
        }

        /// <summary>
        /// Invokes a server method without waiting for a response.
        /// </summary>
        /// <typeparam name="TTarget">The type of the server class.</typeparam>
        /// <param name="deliveryMode">The delivery mode for the method call, indicating the reliability of the call.</param>
        /// <param name="expression">The expression that represents the method to be called on the server.</param>
        public void InvokeForgetServer<TTarget>(DeliveryMode deliveryMode, Expression<Action<TTarget>> expression)
        {
            InvokeForgetRemote(deliveryMode, null, expression);
        }

        /// <summary>
        /// Releases all resources used by <see cref="ClientHub"/>
        /// </summary>
        public override void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }

        void IClientDelegator.OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        void IClientDelegator.OnDisconnected()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        ValueTask IClientDelegator.OnDataReceivedAsync(byte[] data)
        {
            return OnDataRecievedAsync(null, data);
        }

        protected override void SendData(DeliveryMode deliveryMode, int? peerId, byte[] data)
        {
            _client.SendData(deliveryMode, data);
        }
    }
}
