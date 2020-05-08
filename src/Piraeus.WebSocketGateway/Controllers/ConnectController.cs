using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Core.Logging;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Security.Authentication;
using SkunkLab.Security.Tokens;

namespace Piraeus.WebSocketGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly IAuthenticator authn;

        private readonly PiraeusConfig config;

        private readonly GraphManager graphManager;

        private readonly ILog logger;

        private ProtocolAdapter adapter;

        private WebSocket socket;

        private CancellationTokenSource source;

        public ConnectController(PiraeusConfig config, IClusterClient client, ILog logger)
        {
            this.config = config;
            BasicAuthenticator basicAuthn = new BasicAuthenticator();

            SecurityTokenType tokenType = Enum.Parse<SecurityTokenType>(config.ClientTokenType, true);
            basicAuthn.Add(tokenType, config.ClientSymmetricKey, config.ClientIssuer, config.ClientAudience);
            authn = basicAuthn;

            graphManager = new GraphManager(client);
            this.logger = logger;
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Get()
        {
            source = new CancellationTokenSource();
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                    adapter = ProtocolAdapterFactory.Create(config, graphManager, HttpContext, socket, null, authn,
                        source.Token);
                    adapter.OnClose += Adapter_OnClose;
                    adapter.OnError += Adapter_OnError;
                    adapter.Init();
                    await adapter.Channel.OpenAsync();
                    await logger.LogDebugAsync("Websocket channel open.");
                    return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
                }
                catch (Exception ex)
                {
                    StatusCode(500);
                    await logger.LogErrorAsync(ex, "WebSocket get - 500");
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }

            await logger.LogWarningAsync($"WebSocket status code {HttpStatusCode.NotFound}");
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private void Adapter_OnClose(object sender, ProtocolAdapterCloseEventArgs e)
        {
            try
            {
                if (adapter != null && adapter.Channel != null && (adapter.Channel.State == ChannelState.Closed ||
                                                                   adapter.Channel.State == ChannelState.Aborted ||
                                                                   adapter.Channel.State ==
                                                                   ChannelState.ClosedReceived ||
                                                                   adapter.Channel.State == ChannelState.CloseSent))
                {
                    adapter.Dispose();
                    logger.LogDebugAsync("Web socket adapter disposed.").GetAwaiter();
                }
                else
                {
                    try
                    {
                        adapter.Channel.CloseAsync().GetAwaiter();
                        logger.LogDebugAsync("Web socket channel closed.").GetAwaiter();
                    }
                    catch { }

                    adapter.Dispose();
                    logger.LogDebugAsync("Web socket adapter disposed.").GetAwaiter();
                }
            }
            catch { }
        }

        private void Adapter_OnError(object sender, ProtocolAdapterErrorEventArgs e)
        {
            try
            {
                adapter.Channel.CloseAsync().GetAwaiter();
                logger.LogDebugAsync("Web socket adapter disposed.").GetAwaiter();
            }
            catch { }

            logger.LogErrorAsync(e.Error, "Web socket adapter error.").GetAwaiter();
        }
    }
}