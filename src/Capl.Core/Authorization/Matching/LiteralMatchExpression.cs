using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Matching
{
    /// <summary>
    ///     Matches the string literal of a claim type and optional claim value.
    /// </summary>
    public class LiteralMatchExpression : MatchExpression
    {
        public static Uri MatchUri => new Uri(AuthorizationConstants.MatchUris.Literal);

        public override Uri Uri => new Uri(AuthorizationConstants.MatchUris.Literal);

        public override IList<Claim> MatchClaims(IEnumerable<Claim> claims, string claimType, string claimValue)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            ClaimsIdentity ci = new ClaimsIdentity(claims);
            IEnumerable<Claim> claimSet = ci.FindAll(delegate (Claim claim) {
                if (claimValue == null)
                {
                    return claim.Type == claimType;
                }

                return claim.Type == claimType && claim.Value == claimValue;
            });

            return new List<Claim>(claimSet);
        }
    }
}