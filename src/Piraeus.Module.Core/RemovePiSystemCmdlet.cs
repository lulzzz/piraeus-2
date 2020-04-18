using System.Management.Automation;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.Remove, "PiraeusEvent")]
    public class RemovePiSystemCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Unique URI identifier of resource.", Mandatory = true)]
        public string ResourceUriString;

        [Parameter(HelpMessage = "Security token used to access the REST service.", Mandatory = true)]
        public string SecurityToken;

        [Parameter(HelpMessage = "Url of the service.", Mandatory = true)]
        public string ServiceUrl;

        protected override void ProcessRecord()
        {
            string url = string.Format("{0}/api/resource/DeletePiSystem?resourceuristring={1}", ServiceUrl,
                ResourceUriString);
            RestRequestBuilder builder =
                new RestRequestBuilder("DELETE", url, RestConstants.ContentType.Json, true, SecurityToken);
            RestRequest request = new RestRequest(builder);

            request.Delete();
        }
    }
}