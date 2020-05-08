using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Security.Authentication
{
    public class SwtValidationHandler : DelegatingHandler
    {
        private readonly string audience;

        private readonly string issuer;

        private readonly string signingKey;

        public SwtValidationHandler(string signingKey, string issuer = null, string audience = null)
        {
            this.signingKey = signingKey;
            this.audience = audience;
            this.issuer = issuer;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpStatusCode statusCode;

            if (!TryRetrieveToken(request, out string token))
            {
                statusCode = HttpStatusCode.Unauthorized;
                return Task<HttpResponseMessage>.Factory.StartNew(() =>
                    new HttpResponseMessage(statusCode));
            }

            try
            {
                if (SecurityTokenValidator.Validate(token, SecurityTokenType.SWT, signingKey, issuer, audience))
                {
                }

                return base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Exception in SWT validation.");
                Trace.TraceError(ex.Message);
                statusCode = HttpStatusCode.InternalServerError;
            }

            return Task<HttpResponseMessage>.Factory.StartNew(() =>
                new HttpResponseMessage(statusCode));
        }

        private static bool TryRetrieveToken(HttpRequestMessage request, out string token)
        {
            token = null;
            if (!request.Headers.TryGetValues("Authorization", out IEnumerable<string> authzHeaders) ||
                authzHeaders.Count() > 1)
            {
                return false;
            }

            var bearerToken = authzHeaders.ElementAt(0);
            token = bearerToken.StartsWith("Bearer ") ? bearerToken.Substring(7) : bearerToken;
            return true;
        }
    }
}