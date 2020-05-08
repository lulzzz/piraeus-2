using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Security.Authentication;
using SkunkLab.Security.Tokens;

namespace Piraeus.WebSocketGateway.Middleware
{
    public class PiraeusWebSocketMiddleware
    {
        private readonly PiraeusConfig config;

        private readonly Dictionary<string, ProtocolAdapter> container;

        private readonly GraphManager graphManager;

        private readonly ILog logger;

        private readonly RequestDelegate next;

        private readonly WebSocketOptions options;

        private CancellationTokenSource source;

        public PiraeusWebSocketMiddleware(RequestDelegate next, PiraeusConfig config, IClusterClient client,
            ILog logger, IOptions<WebSocketOptions> options)
        {
            container = new Dictionary<string, ProtocolAdapter>();
            this.next = next;
            this.options = options.Value;
            this.config = config;

            graphManager = new GraphManager(client);
            this.logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                return;
            }

            BasicAuthenticator basicAuthn = new BasicAuthenticator();
            SecurityTokenType tokenType = Enum.Parse<SecurityTokenType>(config.ClientTokenType, true);
            basicAuthn.Add(tokenType, config.ClientSymmetricKey, config.ClientIssuer, config.ClientAudience, context);
            IAuthenticator authn = basicAuthn;

            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();

            source = new CancellationTokenSource();
            ProtocolAdapter adapter =
                ProtocolAdapterFactory.Create(config, graphManager, context, socket, logger, authn, source.Token);
            container.Add(adapter.Channel.Id, adapter);
            adapter.OnClose += Adapter_OnClose;
            adapter.OnError += Adapter_OnError;
            adapter.Init();

            await adapter.Channel.OpenAsync();
            await next(context);
            await logger.LogInformationAsync("Exiting Web socket invoke.");
        }

        private void Adapter_OnClose(object sender, ProtocolAdapterCloseEventArgs e)
        {
            logger.LogInformationAsync("Adapter closing.").GetAwaiter();
            ProtocolAdapter adapter = null;

            try
            {
                if (container.ContainsKey(e.ChannelId))
                {
                    adapter = container[e.ChannelId];
                    logger.LogInformationAsync("Adapter on close channel id found adapter to dispose.").GetAwaiter();
                }
                else
                {
                    logger.LogInformationAsync("Adapter on close did not find a channel id available for the adapter.")
                        .GetAwaiter();
                }

                if (adapter != null && adapter.Channel != null && (adapter.Channel.State == ChannelState.Closed ||
                                                                   adapter.Channel.State == ChannelState.Aborted ||
                                                                   adapter.Channel.State ==
                                                                   ChannelState.ClosedReceived ||
                                                                   adapter.Channel.State == ChannelState.CloseSent))
                {
                    adapter.Dispose();
                    logger.LogInformationAsync("Adapter disposed.").GetAwaiter();
                }
                else
                {
                    try
                    {
                        logger.LogInformationAsync("Adpater trying to close channel.").GetAwaiter();
                        adapter.Channel.CloseAsync().GetAwaiter();
                        logger.LogInformationAsync("Adapter has closed the channel").GetAwaiter();
                    }
                    catch { }

                    adapter.Dispose();
                    logger.LogWarningAsync("Adapter disposed by default").GetAwaiter();
                }
            }
            catch (Exception ex)
            {
                logger.LogErrorAsync(ex, "Adapter on close fault").GetAwaiter();
            }
        }

        private void Adapter_OnError(object sender, ProtocolAdapterErrorEventArgs e)
        {
            logger.LogErrorAsync(e.Error, "Adapter OnError").GetAwaiter();

            if (container.ContainsKey(e.ChannelId))
            {
                ProtocolAdapter adapter = container[e.ChannelId];
                adapter.Channel.CloseAsync().GetAwaiter();
                logger.LogWarningAsync("Adapter channel closed due to error.").GetAwaiter();
            }
        }
    }
}