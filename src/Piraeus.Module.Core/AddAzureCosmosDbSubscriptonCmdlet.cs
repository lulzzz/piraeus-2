using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusCosmosDbSubscription")]
    public class AddAzureCosmosDbSubscriptonCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of CosmosDb, e.g, <account>.documents.azure.com:443", Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Name of collection.", Mandatory = true)]
        public string Collection;

        [Parameter(HelpMessage = "Name of database.", Mandatory = true)]
        public string Database;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "CosmosDb read-write key", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "Number of blob storage clients to use.", Mandatory = false)]
        public int NumClients;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string uriString =
                string.Format("https://{0}.documents.azure.com:443?database={1}&collection={2}&clients={3}", Account,
                    Database, Collection, NumClients <= 0 ? 1 : NumClients);

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