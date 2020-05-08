using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Matching
{
    /// <summary>
    ///     Creates a canonical claim that binds to the the RHS of the expression.  The canonical claim is a signal that a
    ///     unary operation
    ///     will be required using only the claim value from the identity.
    /// </summary>
    public class UnaryMatchExpression : MatchExpression
    {
        public static Uri MatchUri => new Uri(AuthorizationConstants.MatchUris.Any);

        public override Uri Uri => new Uri(AuthorizationConstants.MatchUris.Any);

        public override IList<Claim> MatchClaims(IEnumerable<Claim> claims, string claimType, string value)
        {
            Claim claim = new Claim(AuthorizationConstants.MatchUris.Any, "Any");
            return new List<Claim>(new[] { claim });
        }
    }
}