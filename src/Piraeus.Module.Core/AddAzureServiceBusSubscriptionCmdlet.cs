using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusServiceBusSubscription")]
    public class AddAzureServiceBusSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of Servie Bus, i.e., <account>.servicebus.windows.net",
            Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "SAS token used for authentication.", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "Name key used for authentication.", Mandatory = true)]
        public string KeyName;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Service Bus topic send messages.", Mandatory = true)]
        public string Topic;

        protected override void ProcessRecord()
        {
            string uriString = string.Format("sb://{0}.servicebus.windows.net?topic={1}&keyname={2}", Account, Topic,
                KeyName);

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