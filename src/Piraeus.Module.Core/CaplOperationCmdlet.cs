using System;
using System.Management.Automation;
using Capl.Authorization;
using Capl.Authorization.Operations;

namespace Piraeus.Module
{
    [Cmdlet(VerbsCommon.New, "CaplOperation")]
    [OutputType(typeof(EvaluationOperation))]
    public class CaplOperationCmdlet : Cmdlet
    {
        [Parameter(HelpMessage = "Type of operation", Mandatory = true)]
        public OperationType Type;

        [Parameter(HelpMessage = "Value of Operation (optional)", Mandatory = false)]
        public string Value;

        protected override void ProcessRecord()
        {
            Uri operationUri;
            if (Type == OperationType.BetweenDateTime)
            {
                operationUri = BetweenDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.Contains)
            {
                operationUri = ContainsOperation.OperationUri;
            }
            else if (Type == OperationType.Equal)
            {
                operationUri = EqualOperation.OperationUri;
            }
            else if (Type == OperationType.EqualDateTime)
            {
                operationUri = EqualDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.EqualNumeric)
            {
                operationUri = EqualNumericOperation.OperationUri;
            }
            else if (Type == OperationType.Exists)
            {
                operationUri = ExistsOperation.OperationUri;
            }
            else if (Type == OperationType.GreaterThan)
            {
                operationUri = GreaterThanOperation.OperationUri;
            }
            else if (Type == OperationType.GreaterThanDateTime)
            {
                operationUri = GreaterThanOrEqualDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.GreaterThanOrEqual)
            {
                operationUri = GreaterThanOrEqualOperation.OperationUri;
            }
            else if (Type == OperationType.GreaterThanOrEqualDateTime)
            {
                operationUri = GreaterThanOrEqualDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.LessThan)
            {
                operationUri = LessThanOperation.OperationUri;
            }
            else if (Type == OperationType.LessThanDateTime)
            {
                operationUri = LessThanOrEqualDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.LessThanOrEqual)
            {
                operationUri = LessThanOrEqualOperation.OperationUri;
            }
            else if (Type == OperationType.LessThanOrEqualDateTime)
            {
                operationUri = LessThanOrEqualDateTimeOperation.OperationUri;
            }
            else if (Type == OperationType.NotEqual)
            {
                operationUri = NotEqualOperation.OperationUri;
            }
            else
            {
                throw new ArgumentOutOfRangeException("Type");
            }

            EvaluationOperation operation = new EvaluationOperation(operationUri, Value);

            WriteObject(operation);
        }
    }
}