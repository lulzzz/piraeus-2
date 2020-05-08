using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Matching
{
    public abstract class MatchExpression
    {
        public abstract Uri Uri
        {
            get;
        }

        public static MatchExpression Create(Uri matchType, MatchExpressionDictionary matchExpressions)
        {
            _ = matchType ?? throw new ArgumentNullException(nameof(matchType));

            MatchExpression matchExpression;
            if (matchExpressions == null)
            {
                matchExpression =
                    MatchExpressionDictionary.Default
                        [matchType.ToString()]; //CaplConfigurationManager.MatchExpressions[matchType.ToString()];
            }
            else
            {
                matchExpression = matchExpressions[matchType.ToString()];
            }

            return matchExpression;
        }

        public abstract IList<Claim> MatchClaims(IEnumerable<Claim> claims, string claimType, string value);
    }
}