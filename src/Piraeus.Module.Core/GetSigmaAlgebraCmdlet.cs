using System.Collections.Generic;
using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusSigmaAlgebra")]
    public class GetSigmaAlgebraCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Url filter, e.g., http://www.example.org/*/thing?", Mandatory = false)]
        public string Filter;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url;

            if (string.IsNullOrEmpty(Filter))
            {
                url = $"{ServiceUrl}/api/resource/getsigmaalgebra";
            }
            else
            {
                url = $"{ServiceUrl}/api/resource/getsigmaalgebrawithfilter?filter={Filter}";
            }

            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            IEnumerable<string> resourceList = request.Get<IEnumerable<string>>();
            WriteObject(resourceList);
        }
    }
}