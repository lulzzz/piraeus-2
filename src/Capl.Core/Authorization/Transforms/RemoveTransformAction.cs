using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Transforms
{
    /// <summary>
    ///     A transform that removes a claim from the set of claims.
    /// </summary>
    public class RemoveTransformAction : TransformAction
    {
        public static Uri TransformUri => new Uri(AuthorizationConstants.TransformUris.Remove);

        /// <summary>
        ///     The URI that identifies the remove transform action.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.TransformUris.Remove);

        /// <summary>
        ///     Executes the transform.
        /// </summary>
        /// <param name="claimSet">Set of claims to apply the transform.</param>
        /// <param name="sourceClaim">The claim to remove.</param>
        /// <param name="targetClaim">This claim is ignored in the remove transform action.</param>
        /// <returns>Transformed set of claims.</returns>
        public override IEnumerable<Claim> Execute(IEnumerable<Claim> claims, IList<Claim> matchedClaims,
            LiteralClaim targetClaim)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));
            _ = matchedClaims ?? throw new ArgumentNullException(nameof(matchedClaims));

            if (targetClaim != null)
            {
                throw new ArgumentException("The expected value of targetClaim is null.");
            }

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

            List<Claim> claimList = new List<Claim>();

            foreach (Claim claim in claimSet)
                claimList.Remove(claim);

            return claimList.ToArray();
        }
    }
}