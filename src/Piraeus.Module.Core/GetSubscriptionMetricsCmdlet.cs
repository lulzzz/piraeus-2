using System.Management.Automation;
using Piraeus.Core.Messaging;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusSubscriptionMetrics")]
    public class GetSubscriptionMetricsCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        [Parameter(HelpMessage = "Unique URI identifier of subscription.", Mandatory = true)]
        public string SubscriptionUriString;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/Subscription/GetSubscriptionMetrics?subscriptionUriString={1}",
                ServiceUrl, SubscriptionUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            CommunicationMetrics metrics = request.Get<CommunicationMetrics>();

            WriteObject(metrics);
        }
    }
}