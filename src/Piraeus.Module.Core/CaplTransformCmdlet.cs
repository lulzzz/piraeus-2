using System;
using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplTransform")]
    public class CaplTransformCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "An evaluation expression that determines if the transform is applied (optional).",
            Mandatory = false)]
        public Term EvaluationExpression;

        [Parameter(HelpMessage = "Match expression.", Mandatory = true)]
        public Match MatchExpression;

        [Parameter(HelpMessage = "Required claim for 'add' and 'replace' transforms. Not used for 'remove' transform.",
            Mandatory = false)]
        public LiteralClaim TargetClaim;

        [Parameter(HelpMessage = "Type of transform", Mandatory = true)]
        public TransformType Type;

        protected override void ProcessRecord()
        {
            Uri uri;
            if (Type == TransformType.Add)
            {
                uri = new Uri(AuthorizationConstants.TransformUris.Add);
            }
            else if (Type == TransformType.Remove)
            {
                uri = new Uri(AuthorizationConstants.TransformUris.Remove);
            }
            else if (Type == TransformType.Replace)
            {
                uri = new Uri(AuthorizationConstants.TransformUris.Replace);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Type");
            }

            ClaimTransform transform = new ClaimTransform(uri, MatchExpression, TargetClaim)
            {
                Expression = EvaluationExpression
            };

            WriteObject(transform);
        }
    }
}