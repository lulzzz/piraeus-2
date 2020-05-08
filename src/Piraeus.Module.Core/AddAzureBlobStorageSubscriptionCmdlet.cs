using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusBlobStorageSubscription")]
    public class AddAzureBlobStorageSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Account name of Azure Blob Storage, e.g, <account>.blob.core.windows.net",
            Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Type of blob(s) to create, i.e., block, page, append.", Mandatory = true)]
        public AzureBlobType BlobType;

        [Parameter(HelpMessage = "Name of container to write messages.  If omitted writes to $Root.",
            Mandatory = false)]
        public string Container;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "(Optional parameter for Append Blob filename", Mandatory = false)]
        public string Filename;

        [Parameter(HelpMessage = "Either storage key or SAS token for container or account.", Mandatory = true)]
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
            string uriString;
            if (string.IsNullOrEmpty(Filename))
            {
                uriString = string.Format("https://{0}.blob.core.windows.net?container={1}&blobtype={2}&clients={3}",
                    Account, Container, BlobType.ToString(), NumClients <= 0 ? 1 : NumClients);
            }
            else
            {
                uriString = string.Format(
                    "https://{0}.blob.core.windows.net?container={1}&blobtype={2}&clients={3}&file={4}", Account,
                    Container, BlobType.ToString(), NumClients <= 0 ? 1 : NumClients, Filename);
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