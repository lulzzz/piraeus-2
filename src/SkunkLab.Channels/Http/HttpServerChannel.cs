﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.WebApiCompatShim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SkunkLab.Channels.Http
{
    public class HttpServerChannel : HttpChannel
    {
        private readonly string contentType;

        private readonly string endpoint;

        private readonly string resource;

        private readonly string securityToken;

        private ChannelState _state;

        private X509Certificate2 certificate;

        private bool disposed;

        private IEnumerable<KeyValuePair<string, string>> indexes;

        private HttpRequestMessage request;

        /// <summary>
        /// Creates HTTP server channel for a published message or long polling
        /// </summary>
        /// <param name="request">Http request from client.</param>
        //public HttpServerChannel(HttpRequestMessage request)
        public HttpServerChannel(HttpContext context)
        {
            Id = "http-" + Guid.NewGuid().ToString();
            this.request = context.GetHttpRequestMessage();
            Port = request.RequestUri.Port;

            contentType = request.Content.Headers.ContentType != null ? request.Content.Headers.ContentType.MediaType : HttpChannelConstants.CONTENT_TYPE_BYTE_ARRAY;
            IsAuthenticated = context.User.Identity.IsAuthenticated;
            IsConnected = true;
            IsEncrypted = request.RequestUri.Scheme == "https";
        }

        public HttpServerChannel(string endpoint, string resourceUriString, string contentType, IEnumerable<KeyValuePair<string, string>> indexes = null)
        {
            this.endpoint = endpoint;
            this.resource = resourceUriString;
            this.contentType = contentType;
            this.indexes = indexes;
            Id = "http-" + Guid.NewGuid().ToString();
        }

        public HttpServerChannel(string endpoint, string resourceUriString, string contentType, string securityToken, IEnumerable<KeyValuePair<string, string>> indexes = null)
        {
            this.endpoint = endpoint;
            this.resource = resourceUriString;
            this.contentType = contentType;
            this.securityToken = securityToken;
            this.indexes = indexes;
            Id = "http-" + Guid.NewGuid().ToString();
        }

        public HttpServerChannel(string endpoint, string resourceUriString, string contentType, X509Certificate2 certificate, IEnumerable<KeyValuePair<string, string>> indexes = null)
        {
            this.endpoint = endpoint;
            this.resource = resourceUriString;
            this.contentType = contentType;
            this.certificate = certificate;
            this.indexes = indexes;
            Id = "http-" + Guid.NewGuid().ToString();
        }

        public override event EventHandler<ChannelCloseEventArgs> OnClose;

        public override event EventHandler<ChannelErrorEventArgs> OnError;

        public override event EventHandler<ChannelOpenEventArgs> OnOpen;

        public override event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public override event EventHandler<ChannelStateEventArgs> OnStateChange;

        public override string Id { get; internal set; }

        public override bool IsAuthenticated { get; internal set; }

        public override bool IsConnected { get; }

        public override bool IsEncrypted { get; internal set; }

        public override int Port { get; internal set; }

        public override bool RequireBlocking => false;

        public override ChannelState State
        {
            get => _state;

            internal set
            {
                if (value != _state)
                {
                    OnStateChange?.Invoke(this, new ChannelStateEventArgs(Id, value));
                }
                _state = value;
            }
        }

        public override string TypeId => "HTTP";

        public override async Task AddMessageAsync(byte[] message)
        {
            OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            await Task.CompletedTask;
        }

        public override async Task CloseAsync()
        {
            OnClose?.Invoke(this, new ChannelCloseEventArgs(Id));
            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public override async Task OpenAsync()
        {
            OnOpen?.Invoke(this, new ChannelOpenEventArgs(Id, request));
            await Task.CompletedTask;
        }

        public override async Task ReceiveAsync()
        {
            try
            {
                byte[] message = await request.Content.ReadAsByteArrayAsync();

                OnReceive?.Invoke(this, new ChannelReceivedEventArgs(Id, message));
            }
            catch (Exception ex)
            {
                OnError.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
        }

        public override async Task SendAsync(byte[] message)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(endpoint);
                SetSecurityToken(request);
                SetResourceHeader(request);
                SetIndexes(request);

                request.ContentType = contentType;
                request.ContentLength = message.Length;
                request.Method = "POST";

                using Stream stream = request.GetRequestStream();
                await stream.WriteAsync(message, 0, message.Length);
                using HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK ||
response.StatusCode == HttpStatusCode.Accepted ||
response.StatusCode == HttpStatusCode.NoContent)
                {
                    //await Log.LogAsync("Channel {0} sent http request.", this.Id);
                }
                else
                {
                    //invalid response code
                    //await Log.LogWarningAsync("Channel '{0}' invalid response code from send operation of {1}", response.StatusCode);
                }
            }
            catch (WebException we)
            {
                OnError?.Invoke(this, new ChannelErrorEventArgs(Id, we));
            }
            catch (Exception ex)
            {
                OnError.Invoke(this, new ChannelErrorEventArgs(Id, ex));
            }
        }

        protected void Disposing(bool dispose)
        {
            if (!disposed)
            {
                this.request = null;
                this.indexes = null;
                this.certificate = null;
                disposed = true;
            }
        }

        private void SetIndexes(HttpWebRequest request)
        {
            if (indexes != null)
            {
                foreach (var item in indexes)
                {
                    request.Headers.Add(HttpChannelConstants.INDEX_HEADER, item.Key + ";" + item.Value);
                }
            }
        }

        private void SetResourceHeader(HttpWebRequest request)
        {
            if (string.IsNullOrEmpty(resource))
            {
                request.Headers.Add(HttpChannelConstants.RESOURCE_HEADER, resource);
            }
        }

        private void SetSecurityToken(HttpWebRequest request)
        {
            if (!string.IsNullOrEmpty(securityToken))
            {
                request.Headers.Add("Authorization", string.Format("Bearer {0}", securityToken));
                return;
            }

            if (certificate != null)
            {
                request.ClientCertificates.Add(certificate);
            }
        }
    }
}