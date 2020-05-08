using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Crypto.Tls;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Channels.Psk;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Security.Authentication;

namespace Piraeus.Adapters
{
    public class ProtocolAdapterFactory
    {
        public static ProtocolAdapter Create(PiraeusConfig config, GraphManager graphManager, HttpContext context,
            WebSocket socket, ILog logger = null, IAuthenticator authenticator = null,
            CancellationToken token = default)
        {
            WebSocketConfig webSocketConfig = GetWebSocketConfig(config);
            IChannel channel = ChannelFactory.Create(webSocketConfig, context, socket, token);
            string subprotocol = context.WebSockets.WebSocketRequestedProtocols[0];
            if (subprotocol == "mqtt")
            {
                return new MqttProtocolAdapter(config, graphManager, authenticator, channel, logger, context);
            }

            if (subprotocol == "coapV1")
            {
                return new CoapProtocolAdapter(config, graphManager, authenticator, channel, logger);
            }

            throw new InvalidOperationException("invalid web socket subprotocol");
        }

        public static ProtocolAdapter Create(PiraeusConfig config, GraphManager graphManager, HttpContext context,
            ILog logger = null, IAuthenticator authenticator = null, CancellationToken token = default)
        {
            IChannel channel;
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocketConfig webSocketConfig =
                    new WebSocketConfig(config.MaxBufferSize, config.BlockSize, config.BlockSize);
                channel = ChannelFactory.Create(context, webSocketConfig, token);
                if (context.WebSockets.WebSocketRequestedProtocols.Contains("mqtt"))
                {
                    return new MqttProtocolAdapter(config, graphManager, authenticator, channel, logger);
                }

                if (context.WebSockets.WebSocketRequestedProtocols.Contains("coapv1"))
                {
                    return new CoapProtocolAdapter(config, graphManager, authenticator, channel, logger);
                }

                if (context.WebSockets.WebSocketRequestedProtocols.Count == 0)
                {
                    return new WsnProtocolAdapter(config, graphManager, channel, context, logger);
                }

                throw new InvalidOperationException("invalid web socket subprotocol");
            }

            if (context.Request.Method.ToUpperInvariant() != "POST" &&
                context.Request.Method.ToUpperInvariant() != "GET")
            {
                throw new HttpRequestException("Protocol adapter requires HTTP get or post.");
            }

            channel = ChannelFactory.Create(context);
            return new RestProtocolAdapter(config, graphManager, channel, context, logger);
        }

        public static ProtocolAdapter Create(PiraeusConfig config, GraphManager graphManager,
            IAuthenticator authenticator, TcpClient client, ILog logger = null, CancellationToken token = default)
        {
            TlsPskIdentityManager pskManager = null;

            if (!string.IsNullOrEmpty(config.PskStorageType))
            {
                if (config.PskStorageType.ToLowerInvariant() == "redis")
                {
                    pskManager = TlsPskIdentityManagerFactory.Create(config.PskRedisConnectionString);
                }

                if (config.PskStorageType.ToLowerInvariant() == "keyvault")
                {
                    pskManager = TlsPskIdentityManagerFactory.Create(config.PskKeyVaultAuthority,
                        config.PskKeyVaultClientId, config.PskKeyVaultClientSecret);
                }

                if (config.PskStorageType.ToLowerInvariant() == "environmentvariable")
                {
                    pskManager = TlsPskIdentityManagerFactory.Create(config.PskIdentities, config.PskKeys);
                }
            }

            IChannel channel;
            if (pskManager != null)
            {
                channel = ChannelFactory.Create(config.UsePrefixLength, client, pskManager, config.BlockSize,
                    config.MaxBufferSize, token);
            }
            else
            {
                channel = ChannelFactory.Create(config.UsePrefixLength, client, config.BlockSize, config.MaxBufferSize,
                    token);
            }

            IPEndPoint localEP = (IPEndPoint)client.Client.LocalEndPoint;
            int port = localEP.Port;

            if (port == 5684)
            {
                return new CoapProtocolAdapter(config, graphManager, authenticator, channel, logger);
            }

            if (port == 1883 || port == 8883)
            {
                return new MqttProtocolAdapter(config, graphManager, authenticator, channel, logger);
            }

            throw new ProtocolAdapterPortException("TcpClient port does not map to a supported protocol.");
        }

        public static ProtocolAdapter Create(PiraeusConfig config, GraphManager graphManager,
            IAuthenticator authenticator, UdpClient client, IPEndPoint remoteEP, ILog logger = null,
            CancellationToken token = default)
        {
            IPEndPoint endpoint = client.Client.LocalEndPoint as IPEndPoint;

            IChannel channel = ChannelFactory.Create(client, remoteEP, token);
            if (endpoint.Port == 5683)
            {
                return new CoapProtocolAdapter(config, graphManager, authenticator, channel, logger);
            }

            if (endpoint.Port == 5883)
            {
                return new MqttProtocolAdapter(config, graphManager, authenticator, channel, logger);
            }

            throw new ProtocolAdapterPortException("UDP port does not map to a supported protocol.");
        }

        #region configurations

        private static WebSocketConfig GetWebSocketConfig(PiraeusConfig config)
        {
            return new WebSocketConfig(config.MaxBufferSize,
                config.BlockSize,
                config.BlockSize);
        }

        #endregion configurations
    }
}