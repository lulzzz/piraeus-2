using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two strings for equality.
    /// </summary>
    public class EqualOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.Equal);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.Equal);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the arguments are equal string values; otherwise false.</returns>
        public override bool Execute(string left, string right)
        {
            return left == right;
        }
    }
}