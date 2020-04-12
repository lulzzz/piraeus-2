﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Security.Authentication;
using System;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Piraeus.WebSocketGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly IAuthenticator authn;

        private readonly PiraeusConfig config;

        private readonly GraphManager graphManager;

        private readonly ILogger logger;

        private ProtocolAdapter adapter;

        private WebSocket socket;

        private CancellationTokenSource source;

        public ConnectController(PiraeusConfig config, IClusterClient client, ILogger logger)
        {
            this.config = config;
            BasicAuthenticator basicAuthn = new BasicAuthenticator();

            SkunkLab.Security.Tokens.SecurityTokenType tokenType = Enum.Parse<SkunkLab.Security.Tokens.SecurityTokenType>(config.ClientTokenType, true);
            basicAuthn.Add(tokenType, config.ClientSymmetricKey, config.ClientIssuer, config.ClientAudience);
            authn = basicAuthn;

            this.graphManager = new GraphManager(client);
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
                    adapter = ProtocolAdapterFactory.Create(config, graphManager, HttpContext, socket, null, authn, source.Token);
                    adapter.OnClose += Adapter_OnClose;
                    adapter.OnError += Adapter_OnError;
                    adapter.Init();
                    await adapter.Channel.OpenAsync();
                    return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
                }
                catch (Exception ex)
                {
                    StatusCode(500);
                    Console.WriteLine(ex.Message);
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private void Adapter_OnClose(object sender, ProtocolAdapterCloseEventArgs e)
        {
            try
            {
                if ((adapter != null && adapter.Channel != null) && (adapter.Channel.State == ChannelState.Closed || adapter.Channel.State == ChannelState.Aborted || adapter.Channel.State == ChannelState.ClosedReceived || adapter.Channel.State == ChannelState.CloseSent))
                {
                    adapter.Dispose();
                }
                else
                {
                    try
                    {
                        adapter.Channel.CloseAsync().GetAwaiter();
                    }
                    catch { }
                    adapter.Dispose();
                }
            }
            catch { }
        }

        private void Adapter_OnError(object sender, ProtocolAdapterErrorEventArgs e)
        {
            try
            {
                adapter.Channel.CloseAsync().GetAwaiter();
            }
            catch { }
        }
    }
}