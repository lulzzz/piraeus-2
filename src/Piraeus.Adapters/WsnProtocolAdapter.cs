using System;
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Orleans;
using Piraeus.Auditing;
using Piraeus.Configuration;
using Piraeus.Core;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Core.Utilities;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Security.Identity;

namespace Piraeus.Adapters
{
    public class WsnProtocolAdapter : ProtocolAdapter
    {
        private readonly IAuditFactory auditFactory;

        private readonly string cacheKey;

        private readonly PiraeusConfig config;

        private readonly string contentType;

        private readonly GraphManager graphManager;

        private readonly string identity;

        private readonly List<KeyValuePair<string, string>> indexes;

        private readonly List<KeyValuePair<string, string>> localIndexes;

        private readonly ILog logger;

        private readonly IAuditor messageAuditor;

        private readonly string resource;

        private readonly List<string> subscriptions;

        private readonly IAuditor userAuditor;

        private OrleansAdapter adapter;

        private bool closing;

        private bool disposedValue;

        public WsnProtocolAdapter(PiraeusConfig config, GraphManager graphManager, IChannel channel,
            HttpContext context, ILog logger = null)
        {
            this.config = config;
            this.graphManager = graphManager;
            Channel = channel;
            this.logger = logger;

            IdentityDecoder decoder =
                new IdentityDecoder(config.ClientIdentityNameClaimType, context, config.GetClientIndexes());
            identity = decoder.Id;
            localIndexes = decoder.Indexes;

            MessageUri messageUri = new MessageUri(context.Request);
            contentType = messageUri.ContentType;
            cacheKey = messageUri.CacheKey;
            resource = messageUri.Resource;
            subscriptions = messageUri.Subscriptions != null ? new List<string>(messageUri.Subscriptions) : null;
            indexes = messageUri.Indexes != null ? new List<KeyValuePair<string, string>>(messageUri.Indexes) : null;

            auditFactory = AuditFactory.CreateSingleton();
            if (config.AuditConnectionString != null &&
                config.AuditConnectionString.Contains("DefaultEndpointsProtocol"))
            {
                auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "messageaudit"),
                    AuditType.Message);
                auditFactory.Add(new AzureTableAuditor(config.AuditConnectionString, "useraudit"), AuditType.User);
            }
            else if (config.AuditConnectionString != null)
            {
                auditFactory.Add(new FileAuditor(config.AuditConnectionString), AuditType.Message);
                auditFactory.Add(new FileAuditor(config.AuditConnectionString), AuditType.User);
            }

            messageAuditor = auditFactory.GetAuditor(AuditType.Message);
            userAuditor = auditFactory.GetAuditor(AuditType.User);
        }

        public override event EventHandler<ProtocolAdapterCloseEventArgs> OnClose;

        public override event EventHandler<ProtocolAdapterErrorEventArgs> OnError;

        public override event EventHandler<ChannelObserverEventArgs> OnObserve;

        public override IChannel Channel
        {
            get; set;
        }

        public override void Init()
        {
            Channel.OnOpen += Channel_OnOpen;
            Channel.OnReceive += Channel_OnReceive;
            Channel.OnClose += Channel_OnClose;
            Channel.OnError += Channel_OnError;

            Channel.OpenAsync().LogExceptions();
        }

        #region channel events

        private void Adapter_OnObserve(object sender, ObserveMessageEventArgs e)
        {
            MessageAuditRecord record = null;
            int length = 0;
            DateTime sendTime = DateTime.UtcNow;
            try
            {
                byte[] message = ProtocolTransition.ConvertToHttp(e.Message);
                Send(message).LogExceptions();
                OnObserve?.Invoke(this,
                    new ChannelObserverEventArgs(Channel.Id, e.Message.ResourceUri, e.Message.ContentType,
                        e.Message.Message));

                length = message.Length;
                record = new MessageAuditRecord(e.Message.MessageId, identity, Channel.TypeId, "WSN", length,
                    MessageDirectionType.Out, true, sendTime);
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0} - WSN adapter observe error on channel '{1}' with '{2}'",
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH-MM-ss.fffff"), Channel.Id, ex.Message);
                logger?.LogError(ex, $"WSN adapter observe error on channel '{Channel.Id}'.");
                record = new MessageAuditRecord(e.Message.MessageId, identity, Channel.TypeId, "WSN", length,
                    MessageDirectionType.Out, true, sendTime, msg);
            }
            finally
            {
                if (e.Message.Audit)
                {
                    messageAuditor?.WriteAuditRecordAsync(record).Ignore();
                }
            }
        }

        private void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            try
            {
                if (!closing)
                {
                    closing = true;
                    UserAuditRecord record = new UserAuditRecord(Channel.Id, identity, DateTime.UtcNow);
                    userAuditor?.UpdateAuditRecordAsync(record).IgnoreException();
                }

                OnClose?.Invoke(this, new ProtocolAdapterCloseEventArgs(e.ChannelId));
            }
            catch
            {
            }
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            logger?.LogError(e.Error, $"WSN adapter Channel_OnError error on channel '{Channel.Id}'.");
            OnError?.Invoke(this, new ProtocolAdapterErrorEventArgs(Channel.Id, e.Error));
        }

        private void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            if (!Channel.IsAuthenticated)
            {
                OnError?.Invoke(this,
                    new ProtocolAdapterErrorEventArgs(Channel.Id,
                        new SecurityException("Not authenticated on WSN channel")));
                Channel.CloseAsync().Ignore();
                return;
            }

            adapter = new OrleansAdapter(identity, "WebSocket", "WSN", graphManager);
            adapter.OnObserve += Adapter_OnObserve;

            if (subscriptions != null)
            {
                foreach (var sub in subscriptions)
                {
                    SubscriptionMetadata metadata = new SubscriptionMetadata
                    {
                        Identity = identity,
                        Indexes = localIndexes,
                        IsEphemeral = true
                    };

                    adapter.SubscribeAsync(resource, metadata).GetAwaiter();
                }
            }
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs e)
        {
            var metadata = graphManager.GetPiSystemMetadataAsync(resource).GetAwaiter().GetResult();

            EventMessage msg = new EventMessage(contentType, resource, ProtocolType.WSN, e.Message, DateTime.UtcNow,
                metadata.Audit)
            {
                CacheKey = cacheKey
            };

            adapter.PublishAsync(msg, indexes).GetAwaiter();
        }

        private async Task Send(byte[] message)
        {
            try
            {
                if (message.Length > config.MaxBufferSize)
                {
                    logger?.LogErrorAsync(
                        $"Message size {message.Length} is greater than max message size {config.MaxBufferSize}.");
                    OnError.Invoke(this,
                        new ProtocolAdapterErrorEventArgs(Channel.Id, new Exception("Exceeded max message size.")));
                    return;
                }

                await Channel.SendAsync(message);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"WSN adapter send error on channel '{Channel.Id}'.");
            }
        }

        #endregion channel events

        #region Dispose

        public override void Dispose()
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
                    adapter.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion Dispose
    }
}