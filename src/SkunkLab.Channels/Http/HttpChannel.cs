using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SkunkLab.Channels.Http
{
    public abstract class HttpChannel : IChannel
    {
        public abstract event EventHandler<ChannelCloseEventArgs> OnClose;

        public abstract event EventHandler<ChannelErrorEventArgs> OnError;

        public abstract event EventHandler<ChannelOpenEventArgs> OnOpen;

        public abstract event EventHandler<ChannelReceivedEventArgs> OnReceive;

        public abstract event EventHandler<ChannelStateEventArgs> OnStateChange;

        public abstract string Id
        {
            get; internal set;
        }

        public abstract bool IsAuthenticated
        {
            get; internal set;
        }

        public abstract bool IsConnected
        {
            get;
        }

        public abstract bool IsEncrypted
        {
            get; internal set;
        }

        public abstract int Port
        {
            get; internal set;
        }

        public abstract bool RequireBlocking
        {
            get;
        }

        public abstract ChannelState State
        {
            get; internal set;
        }

        public abstract string TypeId
        {
            get;
        }

        public abstract Task AddMessageAsync(byte[] message);

        public abstract Task CloseAsync();

        public abstract void Dispose();

        public abstract Task OpenAsync();

        public abstract Task ReceiveAsync();

        public abstract Task SendAsync(byte[] message);

        #region Client Channels

        public static HttpChannel Create(string endpoint, string securityToken)
        {
            return new HttpClientChannel(endpoint, securityToken);
        }

        public static HttpChannel Create(string endpoint, X509Certificate2 certificate)
        {
            return new HttpClientChannel(endpoint, certificate);
        }

        public static HttpChannel Create(string endpoint, string resourceUriString, string contentType,
            string securityToken, string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            return new HttpClientChannel(endpoint, resourceUriString, contentType, securityToken, cacheKey, indexes);
        }

        public static HttpChannel Create(string endpoint, string resourceUriString, string contentType,
            X509Certificate2 certificate, string cacheKey = null, List<KeyValuePair<string, string>> indexes = null)
        {
            return new HttpClientChannel(endpoint, resourceUriString, contentType, certificate, cacheKey, indexes);
        }

        public static HttpChannel Create(string endpoint, string securityToken, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            return new HttpClientChannel(endpoint, securityToken, observers, token);
        }

        public static HttpChannel Create(string endpoint, X509Certificate2 certificate, IEnumerable<Observer> observers,
            CancellationToken token = default)
        {
            return new HttpClientChannel(endpoint, certificate, observers, token);
        }

        #endregion Client Channels

        #region Server Channels

        public static HttpChannel Create(HttpContext context)
        {
            return new HttpServerChannel(context);
        }

        public static HttpChannel Create(string endpoint, string resourceUriString, string contentType)
        {
            return new HttpServerChannel(endpoint, resourceUriString, contentType);
        }

        public static HttpChannel Create(string endpoint, string resourceUriString, string contentType,
            string securityToken)
        {
            return new HttpServerChannel(endpoint, resourceUriString, contentType, securityToken);
        }

        public static HttpChannel Create(string endpoint, string resourceUriString, string contentType,
            X509Certificate2 certificate)
        {
            return new HttpServerChannel(endpoint, resourceUriString, contentType, certificate);
        }

        #endregion Server Channels
    }
}