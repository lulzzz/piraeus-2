using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplRule")]
    [OutputType(typeof(Rule))]
    public class CaplRuleCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Truthful evaluation of the rule", Mandatory = true)]
        public bool Evaluates;

        [Parameter(HelpMessage = "Name of issuer (optional)", Mandatory = false)]
        public string Issuer;

        [Parameter(HelpMessage = "CAPL Match Expression", Mandatory = true)]
        public Match MatchExpression;

        [Parameter(HelpMessage = "CAPL Operation", Mandatory = true)]
        public EvaluationOperation Operation;

        protected override void ProcessRecord()
        {
            Rule rule = new Rule
            {
                Evaluates = Evaluates,
                Operation = Operation,
                MatchExpression = MatchExpression
            };

            if (!string.IsNullOrEmpty(Issuer))
            {
                rule.Issuer = Issuer;
            }

            WriteObject(rule);
        }
    }
}