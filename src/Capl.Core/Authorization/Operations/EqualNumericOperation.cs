using System;

namespace Capl.Authorization.Operations
{
    /// <summary>
    ///     Compares two decimals for equality.
    /// </summary>
    public class EqualNumericOperation : Operation
    {
        public static Uri OperationUri => new Uri(AuthorizationConstants.OperationUris.EqualNumeric);

        /// <summary>
        ///     Gets the URI that identifies the operation.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.OperationUris.EqualNumeric);

        /// <summary>
        ///     Executes the comparsion.
        /// </summary>
        /// <param name="left">LHS of the expression argument.</param>
        /// <param name="right">RHS of the expression argument.</param>
        /// <returns>True, if the arguments are equal decimal values; othewise false.</returns>
        public override bool Execute(string left, string right)
        {
            DecimalComparer dc = new DecimalComparer();
            return dc.Compare(left, right) == 0;
        }
    }
}