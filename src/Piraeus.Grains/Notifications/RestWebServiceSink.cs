using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Piraeus.Auditing;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.Core.Metadata;
using SkunkLab.Protocols.Coap;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Security.Tokens;
using SecurityTokenType = Piraeus.Core.Metadata.SecurityTokenType;

namespace Piraeus.Grains.Notifications
{
    public class RestWebServiceSink : EventSink
    {
        private readonly string address;

        private readonly string audience;

        private readonly IAuditor auditor;

        private readonly X509Certificate2 certificate;

        private readonly List<Claim> claims;

        private readonly string issuer;

        private string token;

        public RestWebServiceSink(SubscriptionMetadata metadata, List<Claim> claimset = null,
            X509Certificate2 certificate = null, ILog logger = null)
            : base(metadata, logger)
        {
            auditor = AuditFactory.CreateSingleton().GetAuditor(AuditType.Message);
            this.certificate = certificate;

            Uri uri = new Uri(metadata.NotifyAddress);

            NameValueCollection nvc = HttpUtility.ParseQueryString(uri.Query);
            issuer = nvc["issuer"];
            audience = nvc["audience"];
            nvc.Remove("issuer");
            nvc.Remove("audience");

            string uriString = nvc.Count == 0
                ? $"{uri.Scheme}{Uri.SchemeDelimiter}{uri.Authority}{uri.LocalPath}"
                : $"{uri.Scheme}{Uri.SchemeDelimiter}{uri.Authority}{uri.LocalPath}?";

            StringBuilder builder = new StringBuilder();
            builder.Append(uriString);
            for (int i = 0; i < nvc.Count; i++)
            {
                string key = nvc.GetKey(i);
                string value = nvc[key];
                builder.Append($"{key}={value}");
                if (i < nvc.Count - 1)
                {
                    builder.Append("&");
                }
            }

            address = builder.ToString();
            claims = claimset;
        }

        public override async Task SendAsync(EventMessage message)
        {
            AuditRecord record = null;
            HttpWebRequest request = null;
            byte[] payload = null;
            try
            {
                payload = GetPayload(message);
                if (payload == null)
                {
                    await logger.LogWarningAsync($"Rest request '{metadata.SubscriptionUriString}' null payload.");
                    return;
                }

                try
                {
                    request = WebRequest.Create(address) as HttpWebRequest;
                    request.ContentType = message.ContentType;
                    request.Method = "POST";

                    SetSecurityToken(request);

                    request.ContentLength = payload.Length;
                    Stream stream = await request.GetRequestStreamAsync();
                    await stream.WriteAsync(payload, 0, payload.Length);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("REST event sink subscription {0} could set request; error {1} ",
                        metadata.SubscriptionUriString, ex.Message);
                    record = new MessageAuditRecord(message.MessageId, address, "WebService", "HTTP", payload.Length,
                        MessageDirectionType.Out, false, DateTime.UtcNow, ex.Message);
                }

                try
                {
                    using HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                    if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.NoContent)
                    {
                        await logger.LogInformationAsync($"Rest request success {response.StatusCode}");
                        record = new MessageAuditRecord(message.MessageId, address, "WebService", "HTTP",
                            payload.Length, MessageDirectionType.Out, true, DateTime.UtcNow);
                    }
                    else
                    {
                        await logger.LogWarningAsync($"Rest request warning {response.StatusCode}");
                        record = new MessageAuditRecord(message.MessageId, address, "WebService", "HTTP",
                            payload.Length, MessageDirectionType.Out, false, DateTime.UtcNow,
                            string.Format("Rest request returned an expected status code {0}", response.StatusCode));
                    }
                }
                catch (WebException we)
                {
                    string faultMessage =
                        $"subscription '{metadata.SubscriptionUriString}' with status code '{we.Status}' and error message '{we.Message}'";
                    await logger.LogErrorAsync(we, $"Rest request success.");
                    record = new MessageAuditRecord(message.MessageId, address, "WebService", "HTTP", payload.Length,
                        MessageDirectionType.Out, false, DateTime.UtcNow, we.Message);
                }
            }
            catch (Exception ex)
            {
                await logger.LogErrorAsync(ex, $"Rest request success.");
                record = new MessageAuditRecord(message.MessageId, address, "WebService", "HTTP", payload.Length,
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

        private void SetSecurityToken(HttpWebRequest request)
        {
            if (!metadata.TokenType.HasValue || metadata.TokenType.Value == SecurityTokenType.None)
            {
                return;
            }

            if (metadata.TokenType.Value == SecurityTokenType.X509)
            {
                if (certificate == null)
                {
                    throw new InvalidOperationException("X509 client certificates not available to use for REST call.");
                }

                request.ClientCertificates.Add(certificate);
            }
            else if (metadata.TokenType.Value == SecurityTokenType.Jwt)
            {
                JsonWebToken jwt = new JsonWebToken(metadata.SymmetricKey, claims, 20.0, issuer, audience);
                token = jwt.ToString();

                request.Headers.Add("Authorization", string.Format("Bearer {0}", token));

                request.Headers.Add("Authorization", string.Format("Bearer {0}", token));
            }
            else
            {
                throw new InvalidOperationException("No security token type resolved.");
            }
        }
    }
}