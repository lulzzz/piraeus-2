using SkunkLab.Channels;
using System;

namespace Samples.Http.Client
{
    public class HttpObserver : Observer
    {
        public HttpObserver(Uri resourceUri)
        {
            this.ResourceUri = resourceUri;
        }

        public override event ObserverEventHandler OnNotify;

        public override Uri ResourceUri { get; set; }

        public override void Update(Uri resourceUri, string contentType, byte[] message)
        {
            OnNotify?.Invoke(this, new ObserverEventArgs(resourceUri, contentType, message));
        }
    }
}