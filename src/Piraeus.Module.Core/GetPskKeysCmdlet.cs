using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Get, "PiraeusPskKeys")]
    public class GetPskKeysCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/psk/GetKeys", ServiceUrl);
            RestRequestBuilder builder =
                new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);
            string[] keys = request.Get<string[]>();

            WriteObject(keys);
        }
    }
}