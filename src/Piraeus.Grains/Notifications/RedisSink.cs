using System;
using System.Collections.Specialized;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;
using StackExchange.Redis;

namespace Piraeus.Grains.Notifications
{
    public class RedisSink : EventSink
    {
        private readonly IAuditor auditor;

        private readonly string cacheClaimType;

        private readonly string connectionString;

        private readonly ConcurrentQueueManager cqm;

        private readonly int dbNumber;

        private readonly TimeSpan? expiry;

        private readonly TaskQueue tqueue;

        private readonly Uri uri;

        private ConnectionMultiplexer connection;

        private IDatabase database;

        public RedisSink(SubscriptionMetadata metadata, ILog logger = null)
            : base(metadata, logger)
        {
            tqueue = new TaskQueue();
            cqm = new ConcurrentQueueManager();

            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);

            uri = new Uri(metadata.NotifyAddress);

            connectionString = $"{uri.Authority}:6380,password={metadata.SymmetricKey},ssl=True,abortConnect=False";

            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);

            if (!int.TryParse(nvc["db"], out dbNumber))
            {
                dbNumber = -1;
            }

            if (TimeSpan.TryParse(nvc["expiry"], out TimeSpan expiration))
            {
                expiry = expiration;
            }

            if (string.IsNullOrEmpty(metadata.ClaimKey))
            {
                cacheClaimType = metadata.ClaimKey;
            }

            connection = ConnectionMultiplexer.ConnectAsync(connectionString).GetAwaiter().GetResult();
            database = connection.GetDatabase(dbNumber);
        }

        public string GetKey(EventMessage message)
        {
            if (!string.IsNullOrEmpty(message.CacheKey))
            {
                return message.CacheKey;
            }

            if (!string.IsNullOrEmpty(cacheClaimType))
            {
                ClaimsPrincipal principal = Thread.CurrentPrincipal as ClaimsPrincipal;
                ClaimsIdentity identity = new ClaimsIdentity(principal.Claims);
                if (!identity.HasClaim(c => cacheClaimType.ToLowerInvariant() == c.Type.ToLowerInvariant()))
                {
                    return null;
                }

                {
                    Claim claim =
                        identity.FindFirst(
                            c =>
                                c.Type.ToLowerInvariant() ==
                                cacheClaimType.ToLowerInvariant());

                    return claim?.Value;
                }
            }

            return null;
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            byte[] payload = null;
            EventMessage msg = null;

            if (connection == null || !connection.IsConnected)
            {
                connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
                database = connection.GetDatabase(dbNumber);
            }

            await tqueue.Enqueue(() => cqm.EnqueueAsync(message));

            try
            {
                while (!cqm.IsEmpty)
                {
                    msg = await cqm.DequeueAsync();
                    string cacheKey = GetKey(msg);

                    if (cacheKey == null)
                    {
                        await logger?.LogWarningAsync($"Redis sink has no cache key for '{metadata.SubscriptionUriString}'");
                    }

                    payload = GetPayload(msg);

                    if (payload.Length == 0)
                    {
                        throw new InvalidOperationException("Payload length is 0.");
                    }

                    if (msg.ContentType != "application/octet-stream")
                    {
                        Task task = database.StringSetAsync(cacheKey, Encoding.UTF8.GetString(payload), expiry);
                        Task innerTask = task.ContinueWith(async a => { await FaultTask(msg, message.Audit); },
                            TaskContinuationOptions.OnlyOnFaulted);
                        await Task.WhenAll(task);
                    }
                    else
                    {
                        Task task = database.StringSetAsync(cacheKey, payload, expiry);
                        Task innerTask = task.ContinueWith(async a => { await FaultTask(msg, message.Audit); },
                            TaskContinuationOptions.OnlyOnFaulted);
                        await Task.WhenAll(task);
                    }

                    record = new MessageAuditRecord(msg.MessageId,
                        uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                        $"Redis({dbNumber})", string.Format("Redis({0})", dbNumber), payload.Length,
                        MessageDirectionType.Out, true, DateTime.UtcNow);
                }
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync(ex, $"Redis cache sink '{metadata.SubscriptionUriString}'");
                record = new MessageAuditRecord(msg.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                    $"Redis({dbNumber})", string.Format("Redis({0})", dbNumber), payload.Length,
                    MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (message.Audit && record != null)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }
            }
        }

        private async Task FaultTask(EventMessage message, bool canAudit)
        {
            AuditRecord record = null;
            IDatabase db = null;
            byte[] payload = null;
            ConnectionMultiplexer conn = null;

            try
            {
                string cacheKey = GetKey(message);

                conn = await NewConnection();

                if (dbNumber < 1)
                {
                    db = connection.GetDatabase();
                }
                else
                {
                    db = connection.GetDatabase(dbNumber);
                }

                payload = GetPayload(message);

                if (message.ContentType != "application/octet-stream")
                {
                    await db.StringSetAsync(cacheKey, Encoding.UTF8.GetString(payload), expiry);
                }
                else
                {
                    await db.StringSetAsync(cacheKey, payload, expiry);
                }

                record = new MessageAuditRecord(message.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                    $"Redis({db.Database})", $"Redis({db.Database})", payload.Length,
                    MessageDirectionType.Out, true, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync(ex, $"Redis cache sink '{metadata.SubscriptionUriString}'");
                record = new MessageAuditRecord(message.MessageId,
                    uri.Query.Length > 0 ? uri.ToString().Replace(uri.Query, "") : uri.ToString(),
                    $"Redis({db.Database})", $"Redis({db.Database})", payload.Length,
                    MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
            }
            finally
            {
                if (canAudit)
                {
                    await auditor?.WriteAuditRecordAsync(record);
                }

                conn?.Dispose();
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

        private async Task<ConnectionMultiplexer> NewConnection()
        {
            return await ConnectionMultiplexer.ConnectAsync(connectionString);
        }
    }
}