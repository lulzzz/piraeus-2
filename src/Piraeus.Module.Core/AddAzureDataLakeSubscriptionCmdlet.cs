using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusDataLakeSubscription")]
    public class AddAzureDataLakeSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Azure Data Lake Store Account", Mandatory = true)]
        public string Account;

        [Parameter(HelpMessage = "Application ID for access from AAD.", Mandatory = true)]
        public string AppId;

        [Parameter(HelpMessage = "Secret for access from AAD", Mandatory = true)]
        public string ClientSecret;

        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "AAD, e.g, microsoft.onmicrosoft.com", Mandatory = true)]
        public string Domain;

        [Parameter(HelpMessage = "Name of filename to write data, but exclusive of an extension.", Mandatory = false)]
        public string Filename;

        [Parameter(HelpMessage = "Name of folder to write data.", Mandatory = true)]
        public string Folder;

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
            string uriString = Filename == null
                ? string.Format("adl://{0}.azuredatalakestore.net?domain={1}&appid={2}&folder={3}&clients={4}", Account,
                    Domain, AppId, Folder, NumClients <= 0 ? 1 : NumClients)
                : string.Format("adl://{0}.azuredatalakestore.net?domain={1}&appid={2}&folder={3}&file={4}&clients={5}",
                    Account, Domain, AppId, Folder, Filename, NumClients <= 0 ? 1 : NumClients);

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = uriString,
                SymmetricKey = ClientSecret,
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