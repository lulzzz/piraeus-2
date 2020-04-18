using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Capl.Authorization.Transforms
{
    /// <summary>
    ///     A transform that adds a new claim to a set of claims.
    /// </summary>
    public class AddTransformAction : TransformAction
    {
        public static Uri TransformUri => new Uri(AuthorizationConstants.TransformUris.Add);

        /// <summary>
        ///     Gets the URI that identifies the add transform action.
        /// </summary>
        public override Uri Uri => new Uri(AuthorizationConstants.TransformUris.Add);

        /// <summary>
        ///     Executes the add transform action.
        /// </summary>
        /// <param name="claimSet">Set of claims to perform the action.</param>
        /// <param name="sourceClaim">The source claims, which is ignored for the add transform action.</param>
        /// <param name="targetClaim">The target claim to be added with this action.</param>
        /// <returns>A transformed set of claims.</returns>
        public override IEnumerable<Claim> Execute(IEnumerable<Claim> claims, IList<Claim> matchedClaims,
            LiteralClaim targetClaim)
        {
            _ = claims ?? throw new ArgumentNullException(nameof(claims));
            _ = matchedClaims ?? throw new ArgumentNullException(nameof(matchedClaims));
            _ = targetClaim ?? throw new ArgumentNullException(nameof(targetClaim));

            List<Claim> claimList = new List<Claim>(claims);

            Claim claim = new Claim(targetClaim.ClaimType, targetClaim.ClaimValue);
            claimList.Add(claim);

            return claimList.ToArray();
        }
    }
}