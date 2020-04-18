using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two DateTime parameters for equality.
    /// </summary>
    public class EqualDateTimeOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.EqualDateTime);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.EqualDateTime);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the arguments are equal DateTime values.</returns>
        public override bool Execute(string left, string right)
        {
            DateTimeComparer comparer = new DateTimeComparer();
            return comparer.Compare(left, right) == 0;
        }
    }
}