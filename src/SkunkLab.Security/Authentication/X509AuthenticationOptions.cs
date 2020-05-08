using Microsoft.AspNetCore.Authentication;

namespace SkunkLab.Security.Authentication
{
    public class X509AuthenticationOptions : AuthenticationSchemeOptions
    {
        public X509AuthenticationOptions()
        {
        }

        public X509AuthenticationOptions(string storeName, string location, string thumbprint)
        {
            StoreName = storeName;
            Location = location;
            Thumbprint = thumbprint;
        }

        public string Location
        {
            get; set;
        }

        public string Scheme => "SkunkLabX509";

        public string StoreName
        {
            get; set;
        }

        public string Thumbprint
        {
            get; set;
        }
    }
}