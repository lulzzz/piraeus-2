using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two DateTime values to determine if the left argument is greater than the right argument.
    /// </summary>
    public class GreaterThanDateTimeOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.GreaterThanDateTime);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.GreaterThanDateTime);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the LHS argument is greater than the RHS argument DateTime value; otherwise false.</returns>
        public override bool Execute(string left, string right)
        {
            DateTimeComparer comparer = new DateTimeComparer();
            return comparer.Compare(left, right) == 1;
        }
    }
}