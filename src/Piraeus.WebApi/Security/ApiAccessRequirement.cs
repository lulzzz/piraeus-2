using Microsoft.AspNetCore.Authorization;
using AuthorizationPolicy = Capl.Authorization.AuthorizationPolicy;

namespace Piraeus.WebApi.Security
{
    public class ApiAccessRequirement : IAuthorizationRequirement
    {
        public ApiAccessRequirement(AuthorizationPolicy policy)
        {
            Policy = policy;
        }

        public AuthorizationPolicy Policy
        {
            get;
        }
    }
}