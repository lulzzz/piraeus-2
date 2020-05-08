using System;
using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusQueueStorageSubscription")]
    public class AddAzureQueueStorageSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of Azure Queue Storage, e.g, <acconut>.queue.core.windows.net",
            Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Either storage key or SAS token for account or queue.", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "Name of queue to write messages.", Mandatory = true)]
        public string Queue;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Optional TTL for messages to remain in queue.", Mandatory = false)]
        public TimeSpan? TTL;

        protected override void ProcessRecord()
        {
            string uriString = TTL.HasValue
                ? string.Format("https://{0}.queue.core.windows.net?queue={1}&ttl={2}", Account, Queue,
                    TTL.Value.ToString())
                : string.Format("https://{0}.queue.core.windows.net?queue={1}", Account, Queue);

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = uriString,
                SymmetricKey = Key,
                Description = Description
            };

            string url = string.Format("{0}/api/resource/subscribe?resourceuristring={1}", ServiceUrl,
                ResourceUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("POST", url, RestConstants.ContentType.Json, false, SecurityToken);
            RestRequest request = new RestRequest(builder);

            string subscriptionUriString = request.Post<SubscriptionMetadata, string>(metadata);

            WriteObject(subscriptionUriString);
        }
    }
}