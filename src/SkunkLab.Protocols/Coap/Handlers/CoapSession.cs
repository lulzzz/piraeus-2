using System;
using System.Collections.Generic;
using System.Security;
using System.Timers;
using Microsoft.AspNetCore.Http;
using SkunkLab.Security.Identity;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Protocols.Coap.Handlers
{
    public delegate void EventHandler<CoapMessageEventArgs>(object sender, CoapMessageEventArgs args);

    public delegate CoapMessage RespondingEventHandler(object sender, CoapMessageEventArgs args);

    public class CoapSession : IDisposable
    {
        private readonly HttpContext context;

        private readonly Timer keepaliveTimer;

        private string bootstrapToken;

        private SecurityTokenType bootstrapTokenType;

        private bool disposedValue;

        private DateTime keepaliveTimestamp;

        public CoapSession(CoapConfig config, HttpContext context = null)
        {
            Config = config;
            this.context = context;

            CoapReceiver = new Receiver(config.ExchangeLifetime.TotalMilliseconds);
            CoapSender = new Transmitter(config.ExchangeLifetime.TotalMilliseconds,
                config.MaxTransmitSpan.TotalMilliseconds, config.MaxRetransmit);
            CoapSender.OnRetry += Transmit_OnRetry;

            if (config.KeepAlive.HasValue)
            {
                keepaliveTimestamp = DateTime.UtcNow.AddSeconds(config.KeepAlive.Value);
                keepaliveTimer = new Timer(config.KeepAlive.Value * 1000);
                keepaliveTimer.Elapsed += KeepaliveTimer_Elapsed;
                keepaliveTimer.Start();
            }
        }

        public event EventHandler<CoapMessageEventArgs> OnKeepAlive;

        public event EventHandler<CoapMessageEventArgs> OnRetry;

        public Receiver CoapReceiver
        {
            get; internal set;
        }

        public Transmitter CoapSender
        {
            get; internal set;
        }

        public CoapConfig Config
        {
            get; internal set;
        }

        public bool HasBootstrapToken
        {
            get; internal set;
        }

        public string Identity
        {
            get; set;
        }

        public List<KeyValuePair<string, string>> Indexes
        {
            get; set;
        }

        public bool IsAuthenticated
        {
            get; set;
        }

        public bool Authenticate(string tokenType, string token)
        {
            if (HasBootstrapToken)
            {
                IsAuthenticated = Config.Authenticator.Authenticate(bootstrapTokenType, bootstrapToken);
            }
            else
            {
                SecurityTokenType tt = (SecurityTokenType)Enum.Parse(typeof(SecurityTokenType), tokenType, true);
                bootstrapTokenType = tt;
                bootstrapToken = token;
                IsAuthenticated = Config.Authenticator.Authenticate(tt, token);
                HasBootstrapToken = true;
            }

            return IsAuthenticated;
        }

        public bool CanObserve()
        {
            return Config.ConfigOptions.HasFlag(CoapConfigOptions.Observe);
        }

        public void EnsureAuthentication(CoapMessage message, bool force = false)
        {
            if (!IsAuthenticated || force)
            {
                CoapUri coapUri = new CoapUri(message.ResourceUri.ToString());
                if (!Authenticate(coapUri.TokenType, coapUri.SecurityToken))
                {
                    throw new SecurityException("CoAP session not authenticated.");
                }

                IdentityDecoder decoder = new IdentityDecoder(Config.IdentityClaimType, context, Config.Indexes);
                Identity = decoder.Id;
                Indexes = decoder.Indexes;
            }
        }

        public bool IsNoResponse(NoResponseType? messageNrt, NoResponseType result)
        {
            if (!messageNrt.HasValue)
            {
                return false;
            }

            return messageNrt.Value.HasFlag(result);
        }

        public void UpdateKeepAliveTimestamp()
        {
            keepaliveTimestamp = DateTime.UtcNow.AddMilliseconds(Config.KeepAlive.Value);
        }

        private void KeepaliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (keepaliveTimestamp <= DateTime.UtcNow)
            {
                CoapToken token = CoapToken.Create();
                ushort id = CoapSender.NewId(token.TokenBytes);
                CoapRequest ping = new CoapRequest
                {
                    MessageId = id,
                    Token = token.TokenBytes,
                    Code = CodeType.EmptyMessage,
                    MessageType = CoapMessageType.Confirmable
                };

                OnKeepAlive?.Invoke(this, new CoapMessageEventArgs(ping));
            }
        }

        private void Transmit_OnRetry(object sender, CoapMessageEventArgs e)
        {
            OnRetry?.Invoke(this, e);
        }

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (keepaliveTimer != null)
                    {
                        keepaliveTimer.Stop();
                        keepaliveTimer.Dispose();
                    }

                    CoapSender.Dispose();
                    CoapReceiver.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion Dispose
    }
}