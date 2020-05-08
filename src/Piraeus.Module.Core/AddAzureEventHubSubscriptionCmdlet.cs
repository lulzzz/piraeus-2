using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusEventHubSubscription")]
    public class AddAzureEventHubSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of EventHub, e.g, <account>.servicebus.windows.net", Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Name of EventHub", Mandatory = true)]
        public string Hub;

        [Parameter(HelpMessage = "Token used for authentication.", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "Name of key used for authentication.", Mandatory = true)]
        public string KeyName;

        [Parameter(HelpMessage = "Number of blob storage clients to use.", Mandatory = false)]
        public int NumClients;

        [Parameter(HelpMessage = "(Optional) ID of partition if you want to send message to a single partition.",
            Mandatory = false)]
        public string PartitionId;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string uriString = string.Format("eh://{0}.servicebus.windows.net?hub={1}&keyname={2}&clients={3}", Account,
                Hub, KeyName, NumClients <= 0 ? 1 : NumClients);

            if (PartitionId != null)
            {
                uriString = string.Format("{0}&partitionid={1}", uriString, PartitionId);
            }

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