using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Grains;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Piraeus.HttpGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        public ConnectController(PiraeusConfig config, IClusterClient client)
        {
            this.config = config;
            graphManager = new GraphManager(client);
        }

        private byte[] longpollValue;
        private string longpollResource;
        private string contentType;
        private HttpContext context;
        private readonly PiraeusConfig config;
        private readonly GraphManager graphManager;
        private ProtocolAdapter adapter;
        private CancellationTokenSource source;
        private delegate void HttpResponseObserverHandler(object sender, SkunkLab.Channels.ChannelObserverEventArgs args);
        private event HttpResponseObserverHandler OnMessage;

        private readonly WaitHandle[] waitHandles = new WaitHandle[]
        {
            new AutoResetEvent(false)
        };

        [HttpPost]
        public HttpResponseMessage Post()
        {
            this.context = this.HttpContext;
            source = new CancellationTokenSource();
            adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);
            adapter.OnClose += Adapter_OnClose;
            adapter.Init();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        [HttpGet]
        public IActionResult Get()
        {
            this.context = this.HttpContext;
            source = new CancellationTokenSource();
            adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);
            adapter.OnObserve += Adapter_OnObserve;
            adapter.Init();
            ThreadPool.QueueUserWorkItem(new WaitCallback(Listen), waitHandles[0]);
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
        private void Listen(object state)
        {
            AutoResetEvent are = (AutoResetEvent)state;
            OnMessage += (o, a) =>
            {
                longpollValue = a.Message;
                longpollResource = a.ResourceUriString;
                contentType = a.ContentType;
                //context.Response.ContentType = a.ContentType;
                //context.Response.ContentLength = a.Message.Length;
                //context.Response.Headers.Add("x-sl-resource", a.ResourceUriString);
                //context.Response.StatusCode = 200;
                //context.Response.BodyWriter.WriteAsync(a.Message);
                //context.Response.CompleteAsync();
                are.Set();
            };
        }

        private void Adapter_OnObserve(object sender, SkunkLab.Channels.ChannelObserverEventArgs e)
        {
            OnMessage?.Invoke(this, e);
        }

        private void Adapter_OnClose(object sender, ProtocolAdapterCloseEventArgs e)
        {
            adapter.Dispose();
        }
    }
}
