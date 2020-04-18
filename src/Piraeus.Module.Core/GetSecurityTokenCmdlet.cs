using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusManagementToken")]
    public class GetSecurityTokenCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Key used to retreive token.", Mandatory = true)]
        public string Key;

        [Parameter(HelpMessage = "URL of service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/manage?code={1}", ServiceUrl, Key);
            RestRequestBuilder builder = new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true);
            RestRequest request = new RestRequest(builder);

            WriteObject(request.Get<string>());
        }
    }
}