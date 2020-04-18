using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares a value to determine if it is not null.
    /// </summary>
    public class ExistsOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.Exists);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.Exists);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the LHS is not null; otherwise false.</returns>
        public override bool Execute(string left, string right)
        {
            return left != null;
        }
    }
}