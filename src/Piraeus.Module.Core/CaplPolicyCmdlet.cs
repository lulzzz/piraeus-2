using System;
using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplPolicy")]
    public class CaplPolicyCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "(Optional) Determines if the policy should use delegation.", Mandatory = false)]
        public bool Delegation;

        [Parameter(HelpMessage = "Evaluation expression (Rule, LogicalAnd, LogicalOr)", Mandatory = true)]
        public Term EvaluationExpression;

        [Parameter(HelpMessage = "Uniquely identifies the policy as a URI", Mandatory = true)]
        public string PolicyID;

        [Parameter(HelpMessage = "(Optional) transforms", Mandatory = false)]
        public Transform[] Transforms;

        protected override void ProcessRecord()
        {
            AuthorizationPolicy policy = new AuthorizationPolicy(EvaluationExpression, new Uri(PolicyID), Delegation);

            if (Transforms != null && Transforms.Length > 0)
            {
                foreach (Transform transform in Transforms)
                    policy.Transforms.Add(transform);
            }

            WriteObject(policy);
        }
    }
}