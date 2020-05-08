using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Piraeus.Adapters.Utilities;
using Piraeus.Auditing;
using Piraeus.Configuration;
using Piraeus.Core;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using Piraeus.Grains;
using SkunkLab.Channels;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Coap.Handlers;

namespace Piraeus.Adapters
{
    public class CoapRequestDispatcher : ICoapRequestDispatch
    {
        private readonly OrleansAdapter adapter;

        private readonly IAuditor auditor;

        private readonly IChannel channel;

        private readonly PiraeusConfig config;

        private readonly GraphManager graphManager;

        private readonly ILog logger;

        private readonly CoapSession session;

        private Dictionary<string, byte[]> coapObserved;

        private HashSet<string> coapUnobserved;

        private bool disposedValue;

        public CoapRequestDispatcher(CoapSession session, IChannel channel, PiraeusConfig config,
            GraphManager graphManager, ILog logger = null)
        {
            this.channel = channel;
            this.session = session;
            this.config = config;
            this.graphManager = graphManager;
            this.logger = logger;
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            coapObserved = new Dictionary<string, byte[]>();
            coapUnobserved = new HashSet<string>();
            adapter = new OrleansAdapter(session.Identity, channel.TypeId, "CoAP", graphManager, logger);
            adapter.OnObserve += Adapter_OnObserve;
            LoadDurablesAsync().LogExceptions(logger);
        }

        public string Identity
        {
            set => adapter.Identity = value;
        }

        public async Task<CoapMessage> DeleteAsync(CoapMessage message)
        {
            Exception error = null;

            CoapUri uri = new CoapUri(message.ResourceUri.ToString());
            try
            {
                await adapter.UnsubscribeAsync(uri.Resource);
                await logger?.LogDebugAsync($"CoAP delete unsubscribe '{uri.Resource}' for {session.Identity}.");
                coapObserved.Remove(uri.Resource);
                await logger?.LogDebugAsync($"CoAP delete removed '{uri.Resource}' for {session.Identity}.");
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"CoAP delete fault during unsubscribe process for {session.Identity}");
                error = ex;
            }

            if (error == null)
            {
                ResponseMessageType rmt = message.MessageType == CoapMessageType.Confirmable
                    ? ResponseMessageType.Acknowledgement
                    : ResponseMessageType.NonConfirmable;
                await logger?.LogDebugAsync(
                    $"CoAP delete returning response for '{uri.Resource}' with {rmt} for {session.Identity}.");
                return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Deleted, message.Token);
            }

            await logger?.LogDebugAsync(
                $"CoAP delete returning response for '{uri.Resource}' with {ResponseCodeType.EmptyMessage} for {session.Identity}.");
            return new CoapResponse(message.MessageId, ResponseMessageType.Reset, ResponseCodeType.EmptyMessage);
        }

        public async Task<CoapMessage> GetAsync(CoapMessage message)
        {
            await logger?.LogDebugAsync($"CoAP get with RST and empty message for {session.Identity}.");
            return await Task.FromResult<CoapMessage>(new CoapResponse(message.MessageId, ResponseMessageType.Reset,
                ResponseCodeType.EmptyMessage, message.Token));
        }

        public async Task<CoapMessage> ObserveAsync(CoapMessage message)
        {
            if (!message.Observe.HasValue)
            {
                await logger?.LogWarningAsync(
                    $"CoAP observe received without Observe flag and will return RST for {session.Identity}");
                await logger?.LogDebugAsync(
                    $"Returning RST because GET needs to be observe/unobserve for {session.Identity}");
                return new CoapResponse(message.MessageId, ResponseMessageType.Reset, ResponseCodeType.EmptyMessage);
            }

            CoapUri uri = new CoapUri(message.ResourceUri.ToString());
            ResponseMessageType rmt = message.MessageType == CoapMessageType.Confirmable
                ? ResponseMessageType.Acknowledgement
                : ResponseMessageType.NonConfirmable;

            ValidatorResult result = EventValidator.Validate(false, uri.Resource, channel, graphManager);
            if (!result.Validated)
            {
                await logger?.LogErrorAsync($"{result.ErrorMessage} for {session.Identity}");
                return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Unauthorized, message.Token);
            }

            if (!message.Observe.Value)
            {
                await logger?.LogInformationAsync($"CoAP unobserve '{message.ResourceUri}' for {session.Identity}.");
                await adapter.UnsubscribeAsync(uri.Resource);
                await logger?.LogDebugAsync($"CoAP unsubscribed '{message.ResourceUri} for {session.Identity}'.");
                coapObserved.Remove(uri.Resource);
            }
            else
            {
                SubscriptionMetadata metadata = new SubscriptionMetadata
                {
                    IsEphemeral = true,
                    Identity = session.Identity,
                    Indexes = session.Indexes
                };

                await logger?.LogInformationAsync($"CoAP subscribed '{message.ResourceUri}' for {session.Identity}");
                _ = await adapter.SubscribeAsync(uri.Resource, metadata);

                if (!coapObserved.ContainsKey(uri.Resource))
                {
                    coapObserved.Add(uri.Resource, message.Token);
                    await logger?.LogDebugAsync("Key added to observable resource.");
                }
            }

            return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Valid, message.Token);
        }

        public async Task<CoapMessage> PostAsync(CoapMessage message)
        {
            try
            {
                CoapUri uri = new CoapUri(message.ResourceUri.ToString());
                ResponseMessageType rmt = message.MessageType == CoapMessageType.Confirmable
                    ? ResponseMessageType.Acknowledgement
                    : ResponseMessageType.NonConfirmable;
                EventMetadata metadata = await graphManager.GetPiSystemMetadataAsync(uri.Resource);

                ValidatorResult result = EventValidator.Validate(true, metadata, null, graphManager);

                if (!result.Validated)
                {
                    if (metadata.Audit)
                    {
                        await auditor?.WriteAuditRecordAsync(new MessageAuditRecord("XXXXXXXXXXXX", session.Identity,
                                channel.TypeId, "COAP", message.Payload.Length, MessageDirectionType.In, false,
                                DateTime.UtcNow,
                                "Not authorized, missing resource metadata, or channel encryption requirements"))
                            .LogExceptions(logger);
                    }

                    logger?.LogErrorAsync(result.ErrorMessage);
                    return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Unauthorized, message.Token);
                }

                string contentType = message.ContentType.HasValue
                    ? message.ContentType.Value.ConvertToContentType()
                    : "application/octet-stream";

                EventMessage msg = new EventMessage(contentType, uri.Resource, ProtocolType.COAP, message.Encode(),
                    DateTime.UtcNow, metadata.Audit);

                if (!string.IsNullOrEmpty(uri.CacheKey))
                {
                    msg.CacheKey = uri.CacheKey;
                }

                if (uri.Indexes == null)
                {
                    await adapter.PublishAsync(msg);
                }
                else
                {
                    List<KeyValuePair<string, string>> indexes = GetIndexes(uri);
                    await adapter.PublishAsync(msg, indexes);
                }

                return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Created, message.Token);
            }
            catch (Exception ex)
            {
                logger?.LogErrorAsync(ex, $"CoAP POST fault for {session.Identity}.");
                throw ex;
            }
        }

        public async Task<CoapMessage> PutAsync(CoapMessage message)
        {
            CoapUri uri = new CoapUri(message.ResourceUri.ToString());
            ResponseMessageType rmt = message.MessageType == CoapMessageType.Confirmable
                ? ResponseMessageType.Acknowledgement
                : ResponseMessageType.NonConfirmable;

            if (EventValidator.Validate(false, uri.Resource, channel, graphManager).Validated)
            {
                return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Unauthorized, message.Token);
            }

            if (coapObserved.ContainsKey(uri.Resource) || coapUnobserved.Contains(uri.Resource))
            {
                return new CoapResponse(message.MessageId, rmt, ResponseCodeType.NotAcceptable, message.Token);
            }

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = true,
                Identity = session.Identity,
                Indexes = session.Indexes
            };
            _ = await adapter.SubscribeAsync(uri.Resource, metadata);

            coapUnobserved.Add(uri.Resource);

            return new CoapResponse(message.MessageId, rmt, ResponseCodeType.Created, message.Token);
        }

        private void Adapter_OnObserve(object sender, ObserveMessageEventArgs e)
        {
            byte[] message;
            if (coapObserved.ContainsKey(e.Message.ResourceUri))
            {
                message = ProtocolTransition.ConvertToCoap(session, e.Message, coapObserved[e.Message.ResourceUri]);
            }
            else
            {
                message = ProtocolTransition.ConvertToCoap(session, e.Message);
            }

            logger?.LogDebugAsync($"Converted observed CoAP message '{e.Message.ResourceUri}'.");

            Send(message, e).LogExceptions(logger).GetAwaiter();
        }

        private List<KeyValuePair<string, string>> GetIndexes(CoapUri coapUri)
        {
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>(coapUri.Indexes);

            if (coapUri.Indexes.Contains(new KeyValuePair<string, string>("~", "~")))
            {
                list.Remove(new KeyValuePair<string, string>("~", "~"));
                var query = config.GetClientIndexes().Where(ck => ck.Key == session.Config.IdentityClaimType);
                if (query.Count() == 1)
                {
                    query.GetEnumerator().MoveNext();
                    list.Add(new KeyValuePair<string, string>(query.GetEnumerator().Current.Value,
                        "~" + session.Identity));
                }
            }

            return list.Count > 0 ? list : null;
        }

        private async Task LoadDurablesAsync()
        {
            List<string> list = await adapter.LoadDurableSubscriptionsAsync(session.Identity);

            if (list != null)
            {
                coapUnobserved = new HashSet<string>(list);
            }
        }

        private async Task Send(byte[] message, ObserveMessageEventArgs e)
        {
            AuditRecord record = null;
            try
            {
                await channel.SendAsync(message);
                record = new MessageAuditRecord(e.Message.MessageId, session.Identity, channel.TypeId, "COAP",
                    e.Message.Message.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex, $"Fault sending message on channel for {session.Identity}");
                record = new MessageAuditRecord(e.Message.MessageId, session.Identity, channel.TypeId, "COAP",
                    e.Message.Message.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (e.Message.Audit)
                {
                    await auditor?.WriteAuditRecordAsync(record).LogExceptions(logger);
                }
            }
        }

        #region IDisposable Support

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
                    try
                    {
                        adapter.OnObserve -= Adapter_OnObserve;
                        adapter.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger?.LogErrorAsync(ex, $"Disposing Orleans adapter fault for {session.Identity}");
                    }

                    coapObserved.Clear();
                    coapUnobserved.Clear();
                    coapObserved = null;
                    coapUnobserved = null;
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}