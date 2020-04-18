using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two decimal values to determine if the left argument is less than the right argument.
    /// </summary>
    public class LessThanOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.LessThan);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.LessThan);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the LHS argument is less than the RHS argument decimal value; otherwise false.</returns>
        public override bool Execute(string left, string right)
        {
            DecimalComparer dc = new DecimalComparer();
            return dc.Compare(left, right) == -1;
        }
    }
}