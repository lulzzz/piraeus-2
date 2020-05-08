using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;

namespace Piraeus.Grains.Notifications
{
    public class CosmosDBSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly int clientCount;

        private readonly DocumentCollection collection;

        private readonly string collectionId;

        private readonly Database database;

        private readonly string databaseId;

        private readonly int delay;

        private readonly Uri documentDBUri;

        private readonly ConcurrentQueue<EventMessage> queue;

        private readonly DocumentClient[] storageArray;

        private readonly string symmetricKey;

        private readonly Uri uri;

        private int arrayIndex;

        public CosmosDBSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            queue = new ConcurrentQueue<EventMessage>();

            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            uri = new Uri(metadata.NotifyAddress);
            documentDBUri = new Uri($"https://{uri.Authority}");

            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            databaseId = nvc["database"];
            collectionId = nvc["collection"];

            symmetricKey = metadata.SymmetricKey;

            if (!int.TryParse(nvc["clients"], out clientCount))
            {
                clientCount = 1;
            }

            if (!int.TryParse(nvc["delay"], out delay))
            {
                delay = 1000;
            }

            storageArray = new DocumentClient[clientCount];
            for (int i = 0; i < clientCount; i++)
                storageArray[i] = new DocumentClient(documentDBUri, symmetricKey);

            database = GetDatabaseAsync().GetAwaiter().GetResult();

            collection = GetCollectionAsync(database.SelfLink, collectionId).GetAwaiter().GetResult();
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;
            queue.Enqueue(message);
            try
            {
                while (!queue.IsEmpty)
                {
                    arrayIndex = arrayIndex.RangeIncrement(0, clientCount - 1);
                    bool isdequeued = queue.TryDequeue(out EventMessage msg);

                    if (!isdequeued)
                    {
                        continue;
                    }

                    payload = GetPayload(message);
                    if (payload == null)
                    {
                        await logger?.LogWarningAsync(
                            $"Subscription '{metadata.SubscriptionUriString}' message not written to cosmos db sink because message is null.");
                        continue;
                    }

                    await using MemoryStream stream = new MemoryStream(payload)
                    {
                        Position = 0
                    };
                    if (message.ContentType.Contains("json"))
                    {
                        await storageArray[arrayIndex].CreateDocumentAsync(collection.SelfLink,
                            JsonSerializable.LoadFrom<Document>(stream));
                    }
                    else
                    {
                        dynamic documentWithAttachment = new {
                            Id = Guid.NewGuid().ToString(),
                            Timestamp = DateTime.UtcNow
                        };

                        Document doc = await storageArray[arrayIndex]
                            .CreateDocumentAsync(collection.SelfLink, documentWithAttachment);
                        string slug = GetSlug(documentWithAttachment.Id, message.ContentType);
                        await storageArray[arrayIndex].CreateAttachmentAsync(doc.AttachmentsLink, stream,
                            new MediaOptions { ContentType = message.ContentType, Slug = slug });
                    }

                    if (message.Audit)
                    {
                        record = new MessageAuditRecord(message.MessageId,
                            uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                            "CosmosDB", "CosmoDB", payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to cosmos db sink.");
                record = new MessageAuditRecord(message.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(), "CosmosDB",
                    "CosmosDB", payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (record != null && message.Audit)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private async Task<DocumentCollection> GetCollectionAsync(string dbLink, string id)
        {
            List<DocumentCollection> collections = await ReadCollectionsFeedAsync(dbLink);
            if (collections != null)
            {
                foreach (DocumentCollection collection in collections)
                {
                    if (collection.Id == id)
                    {
                        return collection;
                    }
                }
            }

            return await storageArray[0].CreateDocumentCollectionAsync(dbLink, new DocumentCollection { Id = id });
        }

        private async Task<Database> GetDatabaseAsync()
        {
            try
            {
                List<Database> dbs = await ListDatabasesAsync();

                foreach (Database db in dbs)
                {
                    if (db.Id == databaseId)
                    {
                        return db;
                    }
                }

                return await storageArray[0].CreateDatabaseAsync(new Database { Id = databaseId });
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to cosmos db sink failed to get database.");
                throw;
            }
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

        private string GetSlug(string id, string contentType)
        {
            if (contentType.Contains("text"))
            {
                return id + ".txt";
            }

            if (contentType.Contains("xml"))
            {
                return id + ".xml";
            }

            return id;
        }

        private async Task<List<Database>> ListDatabasesAsync()
        {
            string continuation = null;
            List<Database> databases = new List<Database>();

            do
            {
                FeedOptions options = new FeedOptions
                {
                    RequestContinuation = continuation,
                    MaxItemCount = 50
                };

                FeedResponse<Database> response = await storageArray[0].ReadDatabaseFeedAsync(options);
                databases.AddRange(response);

                continuation = response.ResponseContinuation;
            } while (!string.IsNullOrEmpty(continuation));

            return databases;
        }

        private async Task<List<DocumentCollection>> ReadCollectionsFeedAsync(string databaseSelfLink)
        {
            string continuation = null;
            List<DocumentCollection> collections = new List<DocumentCollection>();
            try
            {
                do
                {
                    FeedOptions options = new FeedOptions
                    {
                        RequestContinuation = continuation,
                        MaxItemCount = 50
                    };

                    FeedResponse<DocumentCollection> response =
                        await storageArray[0].ReadDocumentCollectionFeedAsync(databaseSelfLink, options);

                    collections.AddRange(response);

                    continuation = response.ResponseContinuation;
                } while (!string.IsNullOrEmpty(continuation));

                return collections;
            }
            catch (Exception ex)
            {
                await logger?.LogErrorAsync(ex,
                    $"Subscription '{metadata.SubscriptionUriString}' message not written to cosmos db sink, failed to find collection.");
                throw;
            }
        }
    }
}