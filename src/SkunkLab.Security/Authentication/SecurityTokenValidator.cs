using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Security.Authentication
{
    public static class SecurityTokenValidator
    {
        public static bool Validate(string tokenString, SecurityTokenType tokenType, string securityKey,
            string issuer = null, string audience = null, HttpContext context = null)
        {
            if (tokenType == SecurityTokenType.NONE)
            {
                return false;
            }

            if (tokenType == SecurityTokenType.JWT)
            {
                return ValidateJwt(tokenString, securityKey, issuer, audience, context);
            }

            byte[] certBytes = Convert.FromBase64String(tokenString);
            X509Certificate2 cert = new X509Certificate2(certBytes);
            return ValidateCertificate(cert, context);
        }

        private static bool ValidateCertificate(X509Certificate2 cert, HttpContext context = null)
        {
            try
            {
                StoreName storeName = StoreName.My;
                StoreLocation location = StoreLocation.LocalMachine;

                if (X509Util.Validate(storeName, location, X509RevocationMode.Online, X509RevocationFlag.EntireChain,
                    cert, cert.Thumbprint))
                {
                    List<Claim> claimset = X509Util.GetClaimSet(cert);
                    Claim nameClaim = claimset.Find(obj => obj.Type == ClaimTypes.Name);
                    ClaimsIdentity ci = new ClaimsIdentity(claimset);
                    ClaimsPrincipal prin = new ClaimsPrincipal(ci);

                    if (context == null)
                    {
                        Thread.CurrentPrincipal = prin;
                    }
                    else
                    {
                        context.User.AddIdentity(ci);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.TraceError("X509 validation exception '{0}'", ex.Message);
                return false;
            }
        }

        private static bool ValidateJwt(string tokenString, string signingKey, string issuer = null,
            string audience = null, HttpContext context = null)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                TokenValidationParameters validationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateAudience = audience != null,
                    ValidateIssuer = issuer != null,
                    ValidateIssuerSigningKey = true
                };

                ClaimsPrincipal prin =
                    tokenHandler.ValidateToken(tokenString, validationParameters, out SecurityToken stoken);
                if (context == null)
                {
                    Thread.CurrentPrincipal = prin;
                }
                else
                {
                    context.User.AddIdentity(prin.Identity as ClaimsIdentity);
                }

                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("JWT validation exception {0}", ex.Message);
                return false;
            }
        }
    }
}