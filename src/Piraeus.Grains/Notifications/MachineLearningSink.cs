using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;

namespace Piraeus.Grains.Notifications
{
    public class MachineLearningSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly string outputPiSystem;

        private readonly string token;

        private readonly Uri uri;

        public MachineLearningSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            this.logger = logger;
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            token = metadata.SymmetricKey;
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            outputPiSystem = nvc["r"];
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;

            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                HttpContent content = new StringContent(Encoding.UTF8.GetString(message.Message), Encoding.UTF8,
                    "application/json");
                HttpResponseMessage response = await client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                {
                    byte[] outMessage = await response.Content.ReadAsByteArrayAsync();
                    EventMessage output = new EventMessage("application/json", outputPiSystem, message.Protocol,
                        outMessage, DateTime.UtcNow, message.Audit);
                    RaiseOnResponse(new EventSinkResponseArgs(output));
                    record = new MessageAuditRecord(message.MessageId, $"ai://{uri.Authority}", "MachineLearning",
                        "MachineLearning", message.Message.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                }
                else
                {
                    record = new MessageAuditRecord(message.MessageId, $"ai://{uri.Authority}", "MachineLearning",
                        "MachineLearning", message.Message.Length, MessageDirectionType.Out, false, DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription {metadata.SubscriptionUriString} machine learning sink.");
                record = new MessageAuditRecord(message.MessageId, $"ai://{uri.Authority}", "MachineLearning",
                    "MachineLearning", message.Message.Length, MessageDirectionType.Out, false, DateTime.UtcNow);
            }
            finally
            {
                if (record != null && message.Audit)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        protected override void RaiseOnResponse(EventSinkResponseArgs args)
        {
            base.RaiseOnResponse(args);
        }
    }
}