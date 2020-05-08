using Microsoft.AspNetCore.Authentication;

namespace SkunkLab.Security.Authentication
{
    public class JwtAuthenticationOptions : AuthenticationSchemeOptions
    {
        public JwtAuthenticationOptions()
        {
        }

        public JwtAuthenticationOptions(string signingKey, string issuer = null, string audience = null)
        {
            SigningKey = signingKey;
            Issuer = issuer?.ToLowerInvariant();
            Audience = audience?.ToLowerInvariant();
        }

        public string Audience
        {
            get; set;
        }

        public string Issuer
        {
            get; set;
        }

        public string Scheme => "SkunkLabJwt";

        public string SigningKey
        {
            get; set;
        }
    }
}