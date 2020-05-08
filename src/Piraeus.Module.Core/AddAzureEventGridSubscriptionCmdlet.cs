using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusEventGridSubscription")]
    public class AddAzureEventGridSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Host name of EventGrid, e.g., piraeussampletopic.eastus-1.eventgrid.azure.net",
            Mandatory = true)]
        public string Host;

        [Parameter(HelpMessage = "Number of blob storage clients to use.", Mandatory = false)]
        public int NumClients;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Key for EventGrid topic access.", Mandatory = true)]
        public string TopicKey;

        protected override void ProcessRecord()
        {
            string uriString = string.Format("eventgrid://{0}?clients={1}", Host, NumClients <= 0 ? 1 : NumClients);

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = uriString,
                SymmetricKey = TopicKey,
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