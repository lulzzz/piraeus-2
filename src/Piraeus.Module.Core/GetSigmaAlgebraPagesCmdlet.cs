using System.Management.Automation;
using Piraeus.Core.Messaging;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusSigmaAlgebraPages")]
    public class GetSigmaAlgebraPagesCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "ContinuationToken token", Mandatory = true)]
        public ListContinuationToken ContinuationToken;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = $"{ServiceUrl}/api/resource/PageSigmaAlgebra";

            RestRequestBuilder builder =
                new RestRequestBuilder("POST", url, RestConstants.ContentType.Json, false, SecurityToken);
            RestRequest request = new RestRequest(builder);
            ListContinuationToken listToken =
                request.Post<ListContinuationToken, ListContinuationToken>(ContinuationToken);

            WriteObject(listToken);
        }
    }
}