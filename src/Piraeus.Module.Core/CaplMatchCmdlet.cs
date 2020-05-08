using System;
using System.Management.Automation;
using Capl.Authorization;
using Capl.Authorization.Matching;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplMatch")]
    [OutputType(typeof(Match))]
    public class CaplMatchCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Claim type to match", Mandatory = true)]
        public string ClaimType;

        [Parameter(HelpMessage = "Determines whether the claim type is required to match.", Mandatory = true)]
        public bool Required;

        [Parameter(HelpMessage = "Type", Mandatory = true)]
        public MatchType Type;

        [Parameter(HelpMessage = "Value of match expression (optional)", Mandatory = false)]
        public string Value;

        protected override void ProcessRecord()
        {
            Uri matchUri;
            if (Type == MatchType.Literal)
            {
                matchUri = LiteralMatchExpression.MatchUri;
            }
            else if (Type == MatchType.Pattern)
            {
                matchUri = PatternMatchExpression.MatchUri;
            }
            else if (Type == MatchType.ComplexType)
            {
                matchUri = ComplexTypeMatchExpression.MatchUri;
            }
            else if (Type == MatchType.Unary)
            {
                matchUri = UnaryMatchExpression.MatchUri;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Type");
            }

            WriteObject(new Match { ClaimType = ClaimType, Required = Required, Type = matchUri });
        }
    }
}