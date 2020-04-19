using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    public class AddPiraeusWebHookSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Description of the subscription.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Web service endpoint to publish the Web Hook.", Mandatory = true)]
        public string Endpoint;

        [Parameter(HelpMessage = "Base64 encoded HMAC key used to sign request.", Mandatory = true)]
        public string HmacKey;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = $"{ServiceUrl}/api/resource/subscribe?resourceuristring={ResourceUriString}";

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = Endpoint,
                SymmetricKey = HmacKey,
                Description = Description
            };

            RestRequestBuilder builder =
                new RestRequestBuilder("POST", url, RestConstants.ContentType.Json, false, SecurityToken);
            RestRequest request = new RestRequest(builder);

            string subscriptionUriString = request.Post<SubscriptionMetadata, string>(metadata);

            WriteObject(subscriptionUriString);
        }
    }
}