using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplLiteralClaim")]
    [OutputType(typeof(LiteralClaim))]
    public class CaplLiteralClaim : Cmdlet
    {
        [Parameter(HelpMessage = "Claim type of the literal claim.", Mandatory = true)]
        public string ClaimType;

        [Parameter(HelpMessage = "Claim value of the literal claim.", Mandatory = true)]
        public string ClaimValue;

        protected override void ProcessRecord()
        {
            LiteralClaim literalClaim = new LiteralClaim(ClaimType, ClaimValue);
            WriteObject(literalClaim);
        }
    }
}