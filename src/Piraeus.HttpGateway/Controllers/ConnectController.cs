using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Grains;
using SkunkLab.Channels;

namespace Piraeus.HttpGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly PiraeusConfig config;

        private readonly GraphManager graphManager;

        private readonly WaitHandle[] waitHandles = {
            new AutoResetEvent(false)
        };

        private ProtocolAdapter adapter;

        private string contentType;

        private HttpContext context;

        private string longpollResource;

        private byte[] longpollValue;

        private CancellationTokenSource source;

        public ConnectController(PiraeusConfig config, IClusterClient client)
        {
            this.config = config;
            graphManager = new GraphManager(client);
        }

        private delegate void HttpResponseObserverHandler(object sender, ChannelObserverEventArgs args);

        private event HttpResponseObserverHandler OnMessage;

        [HttpGet]
        public IActionResult Get()
        {
            context = HttpContext;
            source = new CancellationTokenSource();
            adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);
            adapter.OnObserve += Adapter_OnObserve;
            adapter.Init();
            ThreadPool.QueueUserWorkItem(Listen, waitHandles[0]);
            WaitHandle.WaitAll(waitHandles);
            Task task = adapter.Channel.CloseAsync();
            Task.WhenAll(task);
            Response.Headers.Add("x-sl-resource", longpollResource);
            Response.ContentLength = longpollValue.Length;
            Response.ContentType = contentType;
            ReadOnlyMemory<byte> rom = new ReadOnlyMemory<byte>(longpollValue);
            Response.BodyWriter.WriteAsync(rom);
            return StatusCode(200);
        }

        [HttpPost]
        public HttpResponseMessage Post()
        {
            context = HttpContext;
            source = new CancellationTokenSource();
            adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);
            adapter.OnClose += Adapter_OnClose;
            adapter.Init();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        private void Adapter_OnClose(object sender, ProtocolAdapterCloseEventArgs e)
        {
            adapter.Dispose();
        }

        private void Adapter_OnObserve(object sender, ChannelObserverEventArgs e)
        {
            OnMessage?.Invoke(this, e);
        }

        private void Listen(object state)
        {
            AutoResetEvent are = (AutoResetEvent)state;
            OnMessage += (o, a) =>
            {
                longpollValue = a.Message;
                longpollResource = a.ResourceUriString;
                contentType = a.ContentType;
                are.Set();
            };
        }
    }
}