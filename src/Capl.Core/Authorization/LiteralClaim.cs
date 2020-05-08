using System;

namespace Capl.Authorization
{
    /// <summary>
    ///     A definition of a claim.
    /// </summary>
    [Serializable]
    public class LiteralClaim
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LiteralClaim" /> class.
        /// </summary>
        public LiteralClaim()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="LiteralClaim" /> class.
        /// </summary>
        /// <param name="claimType">The namespace of the claim.</param>
        /// <param name="claimValue">The value of the claim.</param>
        public LiteralClaim(string claimType, string claimValue)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        /// <summary>
        ///     Gets or sets the claim type.
        /// </summary>
        public string ClaimType
        {
            get; set;
        }

        /// <summary>
        ///     Gets or sets the claim value.
        /// </summary>
        public string ClaimValue
        {
            get; set;
        }
    }
}