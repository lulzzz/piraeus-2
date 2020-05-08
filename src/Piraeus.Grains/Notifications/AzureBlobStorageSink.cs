using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Storage;

namespace Piraeus.Grains.Notifications
{
    public class AzureBlobStorageSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly string blobType;

        private readonly int clientCount;

        private readonly string connectionString;

        private readonly string container;

        private readonly string key;

        private readonly ConcurrentQueue<EventMessage> queue;

        private readonly Uri sasUri;

        private readonly BlobStorage[] storageArray;

        private readonly Uri uri;

        private string appendFilename;

        private int arrayIndex;

        public AzureBlobStorageSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            queue = new ConcurrentQueue<EventMessage>();

            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);

            key = metadata.SymmetricKey;
            uri = new Uri(metadata.NotifyAddress);
            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            container = nvc["container"];

            if (!int.TryParse(nvc["clients"], out clientCount))
            {
                clientCount = 1;
            }

            if (!string.IsNullOrEmpty(nvc["file"]))
            {
                appendFilename = nvc["file"];
            }

            if (string.IsNullOrEmpty(container))
            {
                container = "$Root";
            }

            string btype = nvc["blobtype"];

            blobType = string.IsNullOrEmpty(btype) ? "block" : btype.ToLowerInvariant();

            if (blobType != "block" &&
                blobType != "page" &&
                blobType != "append")
            {
                logger?.LogWarningAsync($"Subscription '{metadata.SubscriptionUriString}' invalid blob type to write.")
                    .GetAwaiter();
                return;
            }

            sasUri = null;
            Uri.TryCreate(metadata.SymmetricKey, UriKind.Absolute, out sasUri);

            storageArray = new BlobStorage[clientCount];
            if (sasUri == null)
            {
                connectionString =
                    $"DefaultEndpointsProtocol=https;AccountName={uri.Authority.Split(new[] { '.' })[0]};AccountKey={key};";

                for (int i = 0; i < clientCount; i++)
                    storageArray[i] = BlobStorage.New(connectionString, 2048, 102400);
            }
            else
            {
                connectionString =
                    $"BlobEndpoint={(container != "$Root" ? uri.ToString().Replace(uri.LocalPath, "") : uri.ToString())};SharedAccessSignature={key}";

                for (int i = 0; i < clientCount; i++)
                    storageArray[i] = BlobStorage.New(connectionString, 2048, 102400);
            }
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;
            EventMessage msg = null;
            queue.Enqueue(message);

            try
            {
                while (!queue.IsEmpty)
                {
                    bool isdequeued = queue.TryDequeue(out msg);
                    if (!isdequeued)
                    {
                        continue;
                    }

                    arrayIndex = arrayIndex.RangeIncrement(0, clientCount - 1);

                    payload = GetPayload(msg);
                    if (payload == null)
                    {
                        await logger?.LogWarningAsync(
                            $"Subscription '{metadata.SubscriptionUriString}' message not written to blob sink because message is null.");
                        return;
                    }

                    string filename = GetBlobName(msg.ContentType);

                    if (blobType != "block")
                    {
                        if (blobType == "page")
                        {
                            int pad = payload.Length % 512 != 0 ? 512 - payload.Length % 512 : 0;
                            byte[] buffer = new byte[payload.Length + pad];
                            Buffer.BlockCopy(payload, 0, buffer, 0, payload.Length);
                            Task task = storageArray[arrayIndex]
                                .WritePageBlobAsync(container, filename, buffer, msg.ContentType);
                            Task innerTask =
                                task.ContinueWith(
                                    async a =>
                                    {
                                        await FaultTask(msg.MessageId, container, filename, payload, msg.ContentType,
                                            msg.Audit);
                                    }, TaskContinuationOptions.OnlyOnFaulted);
                            await Task.WhenAll(task);
                        }
                        else
                        {
                            appendFilename ??= GetAppendFilename(msg.ContentType);

                            byte[] suffix = Encoding.UTF8.GetBytes(Environment.NewLine);
                            byte[] buffer = new byte[payload.Length + suffix.Length];
                            Buffer.BlockCopy(payload, 0, buffer, 0, payload.Length);
                            Buffer.BlockCopy(suffix, 0, buffer, payload.Length, suffix.Length);

                            Task task = storageArray[arrayIndex]
                                .WriteAppendBlobAsync(container, appendFilename, buffer);
                            Task innerTask =
                                task.ContinueWith(
                                    async a =>
                                    {
                                        await FaultTask(msg.MessageId, container, appendFilename, buffer,
                                            msg.ContentType, msg.Audit);
                                    }, TaskContinuationOptions.OnlyOnFaulted);
                            await Task.WhenAll(task);
                        }
                    }
                    else
                    {
                        Task task = storageArray[arrayIndex]
                            .WriteBlockBlobAsync(container, filename, payload, msg.ContentType);
                        Task innerTask =
                            task.ContinueWith(
                                async a =>
                                {
                                    await FaultTask(msg.MessageId, container, filename, payload, msg.ContentType,
                                        msg.Audit);
                                }, TaskContinuationOptions.OnlyOnFaulted);
                        await Task.WhenAll(task);
                    }

                    record = new MessageAuditRecord(msg.MessageId,
                        uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "AzureBlob",
                        "AzureBlob", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}'  message not written to blob sink.");
                record = new MessageAuditRecord(msg.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "AzureBlob",
                    "AzureBlob", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (msg.Audit && record != null)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private async Task FaultTask(string id, string container, string filename, byte[] payload, string contentType,
            bool canAudit)
        {
            AuditRecord record = null;

            try
            {
                BlobStorage storage = BlobStorage.New(connectionString, 2048, 102400);

                if (blobType != "block")
                {
                    if (blobType == "page")
                    {
                        int pad = payload.Length % 512 != 0 ? 512 - payload.Length % 512 : 0;
                        byte[] buffer = new byte[payload.Length + pad];
                        Buffer.BlockCopy(payload, 0, buffer, 0, payload.Length);
                        await storage.WritePageBlobAsync(container, filename, buffer, contentType);
                    }
                    else
                    {
                        await storage.WriteAppendBlobAsync(container, filename, payload);
                    }
                }
                else
                {
                    string[] parts = filename.Split(new[] { '.' });
                    string path2 = parts.Length == 2
                        ? $"{parts[0]}-R.{parts[1]}"
                        : $"{filename}-R";

                    await storage.WriteBlockBlobAsync(container, path2, payload, contentType);
                }

                record = new MessageAuditRecord(id,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "AzureBlob",
                    "AzureBlob", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to blob sink in fault task.");
                record = new MessageAuditRecord(id,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "AzureBlob",
                    "AzureBlob", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (canAudit && record != null)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private string GetAppendFilename(string contentType)
        {
            if (appendFilename != null)
            {
                return appendFilename;
            }

            appendFilename = GetBlobName(contentType);

            return appendFilename;
        }

        private string GetBlobName(string contentType)
        {
            string suffix = null;
            if (contentType.Contains("text"))
            {
                suffix = "txt";
            }
            else if (contentType.Contains("json"))
            {
                suffix = "json";
            }
            else if (contentType.Contains("xml"))
            {
                suffix = "xml";
            }

            string guid = Guid.NewGuid().ToString();
            string filename = $"{guid}T{DateTime.UtcNow.ToString("HH-MM-ss-fffff")}";
            return suffix == null ? filename : $"{filename}.{suffix}";
        }

        private byte[] GetPayload(EventMessage message)
        {
            switch (message.Protocol)
            {
                case ProtocolType.COAP:
                    CoapMessage coap = CoapMessage.DecodeMessage(message.Message);
                    return coap.Payload;

                case ProtocolType.MQTT:
                    MqttMessage mqtt = MqttMessage.DecodeMessage(message.Message);
                    return mqtt.Payload;

                case ProtocolType.REST:
                    return message.Message;

                case ProtocolType.WSN:
                    return message.Message;

                default:
                    return null;
            }
        }
    }
}