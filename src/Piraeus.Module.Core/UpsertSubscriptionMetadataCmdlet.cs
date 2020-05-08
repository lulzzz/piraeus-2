using System;
using System.Collections.Generic;
using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusSubscriptionMetadata")]
    public class UpsertSubscriptionMetadata : Cmdlet
    {
        [Parameter(HelpMessage =
            "(Optional) claim type for the identity used as the cache key.  If omitted, the resource URI query string must contain cachekey parameter and value to set the key.  If query string parameter is used it will override the claim type.")]
        public string ClaimKey;

        [Parameter(HelpMessage = "Optional description of the subsription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Durably persist messages for the TTL when the subsystem is disconnected.",
            Mandatory = false)]
        public bool DurableMessaging;

        [Parameter(HelpMessage = "Expiration of the subscription.", Mandatory = false)]
        public DateTime? Expires;

        [Parameter(
            HelpMessage = "Security identity from claims; required for actively connected subsystems; otherwise omit.",
            Mandatory = false)]
        public string Identity;

        [Parameter(HelpMessage = "Semi-colon delimited list of index keys.  Must match number of index values.",
            Mandatory = false)]
        public string IndexKeys;

        [Parameter(HelpMessage = "Semi-colon delimited list of index keys.  Must match number of index values.",
            Mandatory = false)]
        public string IndexValues;

        [Parameter(HelpMessage = "Required for passively connected subsystems; otherwise omit.", Mandatory = false)]
        public string NotifyAddress;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "The rate retained messages are sent when the subsystem reconnects.",
            Mandatory = false)]
        public TimeSpan? SpoolRate;

        [Parameter(HelpMessage = "Unique URI identifier of subscription.", Mandatory = true)]
        public string SubscriptionUriString;

        [Parameter(
            HelpMessage =
                "Symmetric key if a passively connection subsystem that uses SWT or JWT tokens; otherwise omit.",
            Mandatory = false)]
        public string SymmetricKey;

        [Parameter(HelpMessage = "Type of security token used for passively connected subsystem; otherwise omit.",
            Mandatory = false)]
        public SecurityTokenType? TokenType;

        [Parameter(HelpMessage = "Time-To-Live for retained messages.", Mandatory = false)]
        public TimeSpan? TTL;

        protected override void ProcessRecord()
        {
            List<KeyValuePair<string, string>> kvps = null;

            if (!string.IsNullOrEmpty(IndexKeys) || string.IsNullOrEmpty(IndexValues) ||
                string.IsNullOrEmpty(IndexKeys) || !string.IsNullOrEmpty(IndexValues))
            {
                throw new IndexOutOfRangeException("Index keys and values lengths do not match.");
            }

            if (!string.IsNullOrEmpty(IndexKeys) && !string.IsNullOrEmpty(IndexValues))
            {
                string[] keys = IndexKeys.Split(";", StringSplitOptions.RemoveEmptyEntries);
                string[] values = IndexValues.Split(";", StringSplitOptions.RemoveEmptyEntries);

                if (keys.Length != values.Length)
                {
                    throw new IndexOutOfRangeException("Index keys and values lengths do not match.");
                }

                kvps = new List<KeyValuePair<string, string>>();
                int index = 0;
                while (index < keys.Length)
                {
                    kvps.Add(new KeyValuePair<string, string>(keys[index], values[index]));
                    index++;
                }
            }

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                SubscriptionUriString = SubscriptionUriString,
                Description = Description,
                Identity = Identity,
                Indexes = kvps,
                NotifyAddress = NotifyAddress,
                Expires = Expires,
                TokenType = TokenType,
                TTL = TTL,
                SymmetricKey = SymmetricKey,
                SpoolRate = SpoolRate,
                DurableMessaging = DurableMessaging,
                ClaimKey = ClaimKey
            };

            string url = string.Format("{0}/api/subscription/upsertsubscriptionmetadata", ServiceUrl);
            RestRequestBuilder builder =
                new RestRequestBuilder("PUT", url, RestConstants.ContentType.Json, false, SecurityToken);
            RestRequest request = new RestRequest(builder);

            request.Put(metadata);
        }
    }
}