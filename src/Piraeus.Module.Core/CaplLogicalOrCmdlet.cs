using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplLogicalOr")]
    public class CaplLogicalOrCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Truthful evaluation of the logical OR", Mandatory = true)]
        public bool Evaluates;

        [Parameter(HelpMessage = "Array of Terms (Rules, Logical OR, Logical AND) or any combinations",
            Mandatory = true)]
        public Term[] Terms;

        protected override void ProcessRecord()
        {
            LogicalOrCollection loc = new LogicalOrCollection
            {
                Evaluates = Evaluates
            };
            foreach (Term term in Terms)
                loc.Add(term);

            WriteObject(loc);
        }
    }
}