using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Orleans;
using Piraeus.Adapters;
using Piraeus.Configuration;
using Piraeus.Grains;
using SkunkLab.Channels;

namespace Piraeus.HttpGateway.Middleware
{
    public class PiraeusHttpMiddleware
    {
        private readonly PiraeusConfig config;

        private readonly GraphManager graphManager;

        private readonly RequestDelegate next;

        private readonly WaitHandle[] waitHandles = {
            new AutoResetEvent(false)
        };

        private ProtocolAdapter adapter;

        private HttpContext context;

        private CancellationTokenSource source;

        public PiraeusHttpMiddleware(RequestDelegate next, PiraeusConfig config, IClusterClient client)
        {
            this.next = next;
            this.config = config;
            graphManager = new GraphManager(client);
        }

        private delegate void HttpResponseObserverHandler(object sender, ChannelObserverEventArgs args);

        private event HttpResponseObserverHandler OnMessage;

        public async Task Invoke(HttpContext context)
        {
            source = new CancellationTokenSource();
            adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);
            adapter.OnObserve += Adapter_OnObserve;
            adapter.OnClose += Adapter_OnClose;
            adapter.Init();
            await next(context);
            if (context.Request.Method.ToUpperInvariant() == "GET")
            {
                //long polling
                //adapter = ProtocolAdapterFactory.Create(config, graphManager, context, null, null, source.Token);

                //adapter.Init();
                this.context = context;
                ThreadPool.QueueUserWorkItem(Listen, waitHandles[0]);
                WaitHandle.WaitAll(waitHandles);
                //adapter.Dispose();
            }

            await Task.CompletedTask;
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
                context.Response.ContentType = a.ContentType;
                context.Response.ContentLength = a.Message.Length;
                context.Response.Headers.Add("x-sl-resource", a.ResourceUriString);
                context.Response.StatusCode = 200;
                context.Response.BodyWriter.WriteAsync(a.Message);
                context.Response.CompleteAsync();
                are.Set();
            };
        }
    }
}