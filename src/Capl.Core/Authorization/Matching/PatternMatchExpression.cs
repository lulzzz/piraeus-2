using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Capl.Authorization.Matching
{
    /// <summary>
    ///     Matches the string literal of a claim type and optional regular expression of the claim value.
    /// </summary>
    public class PatternMatchExpression : MatchExpression
    {
        public static Uri MatchUri => new Uri(AuthorizationConstants.MatchUris.Pattern);

        public override Uri Uri => new Uri(AuthorizationConstants.MatchUris.Pattern);

        public override IList<Claim> MatchClaims(IEnumerable<Claim> claims, string claimType, string pattern)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));

            Regex regex = new Regex(pattern);

            ClaimsIdentity ci = new ClaimsIdentity(claims);
            IEnumerable<Claim> claimSet = ci.FindAll(delegate (Claim claim) {
                return claimType == claim.Type;
            });

            if (pattern == null)
            {
                return new List<Claim>(claimSet);
            }

            List<Claim> claimList = new List<Claim>();
            IEnumerator<Claim> en = claimSet.GetEnumerator();

            while (en.MoveNext())
            {
                if (regex.IsMatch(en.Current.Value))
                {
                    claimList.Add(en.Current);
                }
            }

            return claimList;
        }
    }
}