using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Add, "CaplPolicy")]
    public class UpsertCaplPolicyCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "CAPL authorization policy to set.", Mandatory = true)]
        public AuthorizationPolicy Policy;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/accesscontrol/upsertaccesscontrolpolicy", ServiceUrl);
            RestRequestBuilder builder =
                new RestRequestBuilder("PUT", url, RestConstants.ContentType.Xml, false, SecurityToken);
            RestRequest request = new RestRequest(builder);

            request.Put(Policy);
        }
    }
}