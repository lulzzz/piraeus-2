namespace Piraeus.Configuration
{
    public class WebServerConfig : WebConfig
    {
        #region Client Name Claim Type and Indexes

        public string WebIdentityIndexClaimTypes
        {
            get; set;
        }

        public string WebIdentityIndexClaimValues
        {
            get; set;
        }

        public string WebIdentityNameClaimType
        {
            get; set;
        }

        #endregion Client Name Claim Type and Indexes

        #region Client Identity Authentication

        public string WebAudience
        {
            get; set;
        }

        public string WebAuthnCertificateFilename
        {
            get; set;
        }

        public string WebIssuer
        {
            get; set;
        }

        public string WebSecurityTokenType
        {
            get; set;
        }

        public string WebSymmetricKey
        {
            get; set;
        }

        #endregion Client Identity Authentication
    }
}