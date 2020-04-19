using System.Management.Automation;
using Piraeus.Core.Metadata;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "PiraeusMLSubscription")]
    public class AddMachineLearningSubscriptionCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Azure ML service security key (base64 encoded) for Web service call.",
            Mandatory = true)]
        public string AmlSecurityKey;

        [Parameter(HelpMessage = "Azure ML Web service URL to call.", Mandatory = true)]
        public string AmlServiceUrl;

        [Parameter(HelpMessage = "Description of ML service.", Mandatory = false)]
        public string Description;

        [Parameter(HelpMessage = "Output pi-system for ML response.", Mandatory = true)]
        public string OutputResourceUriString;

        [Parameter(HelpMessage = "Unique URI identifier of resource to subscribe.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url =
                $"{ServiceUrl}/api/resource/subscribe?resourceuristring={ResourceUriString}&r={OutputResourceUriString}";

            SubscriptionMetadata metadata = new SubscriptionMetadata
            {
                IsEphemeral = false,
                NotifyAddress = AmlServiceUrl,
                SymmetricKey = AmlSecurityKey,
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