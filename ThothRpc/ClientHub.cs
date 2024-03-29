﻿using System;
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
using ThothRpc.Attributes;

namespace ThothRpc
{
    /// <summary>
    /// Handles all client related operations.
    /// </summary>
    public class ClientHub : Hub, IClientDelegator, IDisposable
    {
        readonly IClient _client;

        /// <summary>
        /// Event that is raised when the client is connected to a server.
        /// </summary>
        public event EventHandler? Connected;

        /// <summary>
        /// Event that is raised when the client is disconnected from the server.
        /// </summary>
        public event EventHandler? Disconnected;

        /// <summary>
        /// Indicates the current connection state with the server.
        /// </summary>
        public ConnectionState ConnectionState => _client.ConnectionState;

        /// <summary>
        /// Initializes a new instance of the ClientHub class with the specified client and configuration.
        /// </summary>
        /// <param name="client">The IClient instance to use for communication with the server.</param>
        /// <param name="config">The IHubConfiguration instance to use for configuring the hub.</param>
        public ClientHub(IClient client, IHubConfiguration config) 
            : base(true, config)
        {
            Guard.AgainstNull(nameof(client), client);
            _client = client;

            _client.Init(
                delegator: this,
                connectingTimeout: config.ConnectingTimeout,
                requestHandling: config.RequestHandlingStrategy,
                disconnectTimeout: config.DisconnectTimeout
            );
        }

        /// <summary>
        /// Initializes a new instance of the ClientHub class with the specified server hub and configuration.
        /// This allows the client to communicate with a locally hosted server hub.
        /// </summary>
        /// <param name="localServer">The ServerHub instance to use as the local server.</param>
        /// <param name="config">The IHubConfiguration instance to use for configuring the hub.</param>
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
        /// <exception cref="InvalidOperationException">Thrown if this method is called when the client is attached to a local server.</exception>
        public void ProcessRequests()
        {
            if (_client == null)
            {
                throw new InvalidOperationException
                    ("This method is unallowed when attached to a local server.");
            }

            _client.ProcessRequests();
        }

        /// <summary>
        /// Asynchronously connects the client to a server at the specified address and port, using the provided connection key.
        /// </summary>
        /// <param name="address">The address of the server to connect to.</param>
        /// <param name="port">The port of the server to connect to.</param>
        /// <param name="connectionKey">The connection key to use for authentication with the server.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called when the client is attached to a local server.</exception>
        public Task ConnectAsync(string address, int port, string connectionKey)
        {
            if (_client == null)
            {
                throw new InvalidOperationException
                    ("Client cannot be connected to a server when one is already attached locally.");
            }

            return _client.ConnectAsync(address, port, connectionKey);
        }

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public void Disconnect()
        {
            _client.Disconnect();
        }

        /// <summary>
        /// Registers an object instance with the client hub. This allows the instance to be called from the server.
        /// All public methods decorated with the <see cref="ThothMethodAttribute"/> will be used unless methodNames
        /// is specified, in which case method names provided will be used.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="targetName">(Optional) - Defaults to instance type name.</param>
        /// <param name="methodNames">If specified, methods listed will be marked as accessible by peers.</param>
        /// <exception cref="InvalidOperationException">Thrown if the target has already been registered under the specified name.</exception>
        public void Register(object instance, string? targetName = null, IEnumerable<string>? methodNames = null)
        {
            RegisterBase(instance, targetName, methodNames);
        }

        /// <summary>
        /// <para>
        /// Registers an object instance with the client hub. This allows the instance to be called from the server.
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

        public ValueTask<TResult> InvokeServerAsync<TTarget, TResult>(Expression<Func<TTarget, Task<TResult>>> expression,
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
        /// Invokes a server method and returns the result. 
        /// </summary>
        /// <typeparam name="TTarget">The type of the server class.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="expression">An expression that specifies the method to be called on the server.</param>
        /// <returns>The return value of the method.</returns>
        public TResult InvokeServer<TTarget, TResult>(Expression<Func<TTarget, TResult>> expression)
        {
            return InvokeRemote(null, expression);
        }

        /// <summary>
        /// Invokes a server method.
        /// </summary>
        /// <typeparam name="TTarget">The type of the server class.</typeparam>
        /// <param name="expression">An expression that specifies the method to be called on the server.</param>
        public void InvokeServer<TTarget>(Expression<Action<TTarget>> expression)
        {
            InvokeRemote(null, expression);
        }

        /// <summary>
        /// Invokes a server method and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        /// <returns>The return value of the method.</returns>
        public TResult InvokeServer<TResult>(string targetClass, string method, params object[] parameters)
        {
            return InvokeRemote<TResult>(null, targetClass, method, parameters);
        }

        /// <summary>
        /// Invokes a server method.
        /// </summary>
        /// <param name="targetClass">The name of the server class.</param>
        /// <param name="method">The name of the method to be called on the server.</param>
        /// <param name="parameters">The parameters to be passed to the method.</param>
        public void InvokeServer(string targetClass, string method, params object[] parameters)
        {
            InvokeRemote(null, targetClass, method, null, parameters);
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
        /// Releases all resources used by <see cref="ClientHub"/>. Disconnects the client from server.
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

        ValueTask IClientDelegator.OnDataReceivedAsync(ReadOnlyMemory<byte> data)
        {
            return OnDataRecievedAsync(null, data);
        }

        protected override Task SendDataAsync(DeliveryMode deliveryMode, int? peerId, byte[] data)
        {
            return _client.SendDataAsync(deliveryMode, data);
        }
    }
}
