using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Transforms
{
    /// <summary>
    ///     A transform action that replaces the source claim with the target claim from the set of claims.
    /// </summary>
    public class ReplaceTransformAction : TransformAction
    {
        public static Uri TransformUri => new Uri(AuthorizationConstants.TransformUris.Replace);

        /// <summary>
        ///     Gets the URI that identifies the replace transform action.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.TransformUris.Replace);

        /// <summary>
        ///     Executes the replacement transform.
        /// </summary>
        /// <param name="claimSet">The set of claims to perform the action.</param>
        /// <param name="sourceClaim">The claim to be replaced.</param>
        /// <param name="targetClaim">The claim to replace the source claim.</param>
        /// <returns>Transformed set of claims.</returns>
        public override IEnumerable<Claim> Execute(IEnumerable<Claim> claims, IList<Claim> matchedClaims,
            LiteralClaim targetClaim)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));
            _ = matchedClaims ?? throw new ArgumentNullException(nameof(matchedClaims));
            _ = targetClaim ?? throw new ArgumentNullException(nameof(targetClaim));

            ClaimsIdentity ci = new ClaimsIdentity(claims);
            IEnumerable<Claim> claimSet = ci.FindAll(delegate (Claim claim) {
                foreach (Claim c in matchedClaims)
                {
                    if (c.Type == claim.Type && c.Value == claim.Value)
                    {
                        return true;
                    }
                }

                return false;
            });

            List<Claim> claimList = new List<Claim>(claimSet);
            List<string> valueList = new List<string>();

            foreach (Claim claim in claimSet)
            {
                valueList.Add(claim.Value);
                claimList.Remove(claim);
            }

            if (claimList.Count > 0)
            {
                if (targetClaim.ClaimValue == null)
                {
                    int index = 0;
                    while (index < valueList.Count)
                    {
                        claimList.Add(new Claim(targetClaim.ClaimType, valueList[index]));
                        index++;
                    }
                }
                else
                {
                    claimList.Add(new Claim(targetClaim.ClaimType, targetClaim.ClaimValue));
                }
            }

            return claimList.ToArray();
        }
    }
}