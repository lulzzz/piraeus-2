using System.Management.Automation;
using Capl.Authorization;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplLogicalAnd")]
    public class CaplLogicalAnd : Cmdlet
    {
        [Parameter(HelpMessage = "Truthful evaluation of the logical AND", Mandatory = true)]
        public bool Evaluates;

        [Parameter(HelpMessage = "Array of Terms (Rules, Logical OR, Logical AND) or any combinations",
            Mandatory = true)]
        public Term[] Terms;

        protected override void ProcessRecord()
        {
            LogicalAndCollection lac = new LogicalAndCollection
            {
                Evaluates = Evaluates
            };
            foreach (Term term in Terms)
                lac.Add(term);

            WriteObject(lac);
        }
    }
}