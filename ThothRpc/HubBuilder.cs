using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThothRpc.Base;

namespace ThothRpc
{
    /// <summary>
    /// A builder class for creating instances of <see cref="ClientHub"/>.
    /// </summary>
    public class ClientHubBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="ClientHubBuilder"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="ClientHubBuilder"/>.</returns>
        public static ClientHubBuilder BuildClient() => new ClientHubBuilder();

        /// <summary>
        /// Gets or sets the transport for the <see cref="ClientHub"/>.
        /// </summary>
        public IClient Transport { get; set; } = null!;

        /// <summary>
        /// Gets or sets the configuration for the <see cref="ClientHub"/>.
        /// </summary>
        public HubConfiguration HubConfiguration { get; set; } = new HubConfiguration(null!, null!);

        /// <summary>
        /// Gets or sets an optional local server for the <see cref="ClientHub"/>.
        /// </summary>
        public ServerHub? LocalServer { get; set; }

        /// <summary>
        /// Sets the transport for the <see cref="ClientHub"/> to be an instance of the specified transport type.
        /// The transport type must have a public parameterless constructor.
        /// </summary>
        /// <typeparam name="TransportT">The type of transport to use.</typeparam>
        /// <returns>The current instance of <see cref="ClientHubBuilder"/>.</returns>
        /// <exception cref="Exception">Thrown if the specified transport type does not have a public parameterless constructor.</exception>
        public ClientHubBuilder UseTransport<TransportT>() where TransportT : IClient
        {
            var hasConstructor = typeof(TransportT).GetConstructor(Type.EmptyTypes) != null;

            if (!hasConstructor)
            {
                throw new Exception($"{typeof(TransportT).Name} must have a public parameterless constructor to be created via " +
                    "the UseTransport method. You may need to create the client manually.");
            }

            Transport = (TransportT)Activator.CreateInstance(typeof(TransportT))!;
            return this;
        }

        /// <summary>
        /// Sets the transport to the given instance.
        /// </summary>
        /// <param name="transport">Transport instance to use.</param>
        /// <returns>The current instance of <see cref="ClientHubBuilder"/>.</returns>
        public ClientHubBuilder UseTransport(IClient transport)
        {
            Transport = transport;
            return this;
        }

        /// <summary>
        /// Modifies the configuration for the <see cref="ClientHub"/>.
        /// </summary>
        /// <param name="configModify">Action to modify configuration.</param>
        /// <returns>The current instance of <see cref="ClientHubBuilder"/>.</returns>
        public ClientHubBuilder WithConfiguration(Action<HubConfiguration> configModify)
        {
            configModify(HubConfiguration);
            return this;
        }

        /// <summary>
        /// Sets the local server for the <see cref="ClientHub"/>. This will configure the ClientHub to not make calls over the network
        /// and instead will be configured to invoke the given <see cref="ServerHub"/> in process.
        /// </summary>
        /// <param name="server">The local server to use for the <see cref="ClientHub"/>.</param>
        /// <returns>The current instance of <see cref="ClientHubBuilder"/>.</returns>
        public ClientHubBuilder UseLocalServer(ServerHub server)
        {
            LocalServer = server;
            return this;
        }

        /// <summary>
        /// Builds an instance of <see cref="ClientHub"/> using the specified transport, configuration, and local server (if set).
        /// </summary>
        /// <returns>An instance of <see cref="ClientHub"/>.</returns>
        /// <exception cref="Exception">Thrown if the configuration is invalid.</exception>
        public ClientHub Build()
        {
            if (HubConfiguration == null)
            {
                throw new Exception("HubConfiguration has not been set and is required.");
            }

            if (Transport == null && LocalServer == null)
            {
                throw new Exception("Transport has not been set and is required.");
            }

            if (Transport != null && LocalServer != null)
            {
                throw new Exception("Transport cannot be specified along with a local server configuration.");
            }

            if (LocalServer == null)
            {
                return new ClientHub(Transport!, HubConfiguration);
            }
            else
            {
                return new ClientHub(LocalServer, HubConfiguration);
            }
        }
    }

    /// <summary>
    /// A builder class for creating instances of <see cref="ServerHub"/>.
    /// </summary>
    public class ServerHubBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="ServerHubBuilder"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="ServerHubBuilder"/>.</returns>
        public static ServerHubBuilder BuildServer() => new ServerHubBuilder();

        /// <summary>
        /// Gets or sets the transport for the <see cref="ServerHub"/>.
        /// </summary>
        public IServer Transport { get; set; } = null!;

        /// <summary>
        /// Gets or sets the configuration for the <see cref="ServerHub"/>.
        /// </summary>
        public HubConfiguration HubConfiguration { get; set; } = new HubConfiguration(null!, null!);

        /// <summary>
        /// Sets the transport for the <see cref="ServerHub"/> to be an instance of the specified transport type.
        /// The transport type must have a public parameterless constructor.
        /// </summary>
        /// <typeparam name="TransportT">The type of transport to use.</typeparam>
        /// <returns>The current instance of <see cref="ServerHubBuilder"/>.</returns>
        /// <exception cref="Exception">Thrown if the specified transport type does not have a public parameterless constructor.</exception>
        public ServerHubBuilder UseTransport<TransportT>() where TransportT : IServer
        {
            var hasConstructor = typeof(TransportT).GetConstructor(Type.EmptyTypes) != null;

            if (!hasConstructor)
            {
                throw new Exception($"{typeof(TransportT).Name} must have a public parameterless constructor to be created via " +
                    "the UseTransport method. You may need to create the server manually.");
            }

            Transport = (TransportT)Activator.CreateInstance(typeof(TransportT))!;
            return this;
        }

        /// <summary>
        /// Sets the transport to the given instance.
        /// </summary>
        /// <param name="transport">Transport instance to use.</param>
        /// <returns>The current instance of <see cref="ServerHubBuilder"/>.</returns>
        public ServerHubBuilder UseTransport(IServer transport)
        {
            Transport = transport;
            return this;
        }

        /// <summary>
        /// Modifies the configuration for the <see cref="ServerHub"/>.
        /// </summary>
        /// <param name="configModify">Action to modify configuration.</param>
        /// <returns>The current instance of <see cref="ServerHubBuilder"/>.</returns>
        public ServerHubBuilder WithConfiguration(Action<HubConfiguration> configModify)
        {
            configModify(HubConfiguration);
            return this;
        }

        /// <summary>
        /// Builds an instance of <see cref="ServerHub"/> using the specified transport and configuration.
        /// </summary>
        /// <returns>An instance of <see cref="ServerHub"/>.</returns>
        /// <exception cref="Exception">Thrown if the configuration is invalid.</exception>
        public ServerHub Build()
        {
            if (HubConfiguration == null)
            {
                throw new Exception("HubConfiguration has not been set and is required.");
            }

            if (Transport == null)
            {
                throw new Exception("Transport has not been set and is required.");
            }

            return new ServerHub(Transport, HubConfiguration);
        }
    }
}
