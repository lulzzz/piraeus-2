﻿namespace SkunkLab.Security.Authentication
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.IdentityModel.Tokens;
    using SkunkLab.Security.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;

    public static class SecurityTokenValidator
    {
        public static bool Validate(string tokenString, SecurityTokenType tokenType, string securityKey, string issuer = null, string audience = null, HttpContext context = null)
        {
            if (tokenType == SecurityTokenType.NONE)
            {
                return false;
            }

            if (tokenType == SecurityTokenType.JWT)
            {
                return ValidateJwt(tokenString, securityKey, issuer, audience, context);
            }
            else
            {
                byte[] certBytes = Convert.FromBase64String(tokenString);
                X509Certificate2 cert = new X509Certificate2(certBytes);
                return ValidateCertificate(cert, context);
            }
        }

        private static bool ValidateCertificate(X509Certificate2 cert, HttpContext context = null)
        {
            try
            {
                StoreName storeName = StoreName.My;
                StoreLocation location = StoreLocation.LocalMachine;

                if (X509Util.Validate(storeName, location, X509RevocationMode.Online, X509RevocationFlag.EntireChain, cert, cert.Thumbprint))
                {
                    List<Claim> claimset = X509Util.GetClaimSet(cert);
                    Claim nameClaim = claimset.Find((obj) => obj.Type == System.Security.Claims.ClaimTypes.Name);
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
                    //GenericIdentity identity = new GenericIdentity(nameClaim.Value);
                    //identity.AddClaims(claimset);
                    //Thread.CurrentPrincipal = new GenericPrincipal(identity, null);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(string.Format("X509 validation exception '{0}'", ex.Message));
                return false;
            }
        }

        private static bool ValidateJwt(string tokenString, string signingKey, string issuer = null, string audience = null, HttpContext context = null)
        {
            try
            {
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();

                TokenValidationParameters validationParameters = new TokenValidationParameters()
                {
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    ValidateAudience = audience != null,
                    ValidateIssuer = issuer != null,
                    ValidateIssuerSigningKey = true
                };


                ClaimsPrincipal prin = tokenHandler.ValidateToken(tokenString, validationParameters, out SecurityToken stoken);
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

        //private static bool ValidateSwt(string tokenString, string securityKey, string issuer = null, string audience = null)
        //{
        //    bool result = false;

        //    try
        //    {
        //        SimpleWebToken token = SimpleWebToken.FromString(tokenString);
        //        if(!token.SignVerify(Convert.FromBase64String(securityKey)))
        //        {
        //            throw new System.Security.SecurityException("SWT cannot be verified.");
        //        }

        //        if(audience != null && token.Audience.ToLower(CultureInfo.InvariantCulture) != audience.ToLower(CultureInfo.InvariantCulture))
        //        {
        //            throw new System.Security.SecurityException("SWT audience mismatch.");
        //        }

        //        if(issuer != null && token.Issuer.ToLower(CultureInfo.InvariantCulture) != issuer.ToLower(CultureInfo.InvariantCulture))
        //        {
        //            throw new System.Security.SecurityException("SWT issuer mismatch.");
        //        }

        //        if(token.ExpiresOn < DateTime.UtcNow)
        //        {
        //            throw new System.Security.SecurityException("SWT token has expired.");
        //        }

        //        ClaimsPrincipal principal = new ClaimsPrincipal(token.Identity);
        //        Thread.CurrentPrincipal = principal;
        //    }
        //    catch(Exception ex)
        //    {
        //        Trace.TraceWarning("SWT validation exception.");
        //        Trace.TraceError(ex.Message);
        //    }

        //    return result;
        //}
    }
}