using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     An abstract operation that performs an authorization function.
    /// </summary>
    public abstract class Operation
    {
        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public abstract Uri Uri
        {
            get;
        }

        /// <summary>
        ///     Creates an AuthorizationOperation used to compare values.
        /// </summary>
        /// <param name="operationUri">Uri if the operation.</param>
        /// <param name="operations">A dictionary of operations.  The value may be null.</param>
        /// <returns>An AuthorizationOperation to compare values.</returns>
        public static Operation Create(Uri operationUri, OperationsDictionary operations)
        {
            _ = operationUri ?? throw new ArgumentNullException(nameof(operationUri));

            Operation operation;
            if (operations == null)
            {
                operation = OperationsDictionary.Default[
                    operationUri.ToString()]; //CaplConfigurationManager.Operations[operationUri.ToString()];
            }
            else
            {
                operation = operations[operationUri.ToString()];
            }

            return operation;
        }

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the compare is ture; otherwise false.</returns>
        public abstract bool Execute(string left, string right);
    }
}