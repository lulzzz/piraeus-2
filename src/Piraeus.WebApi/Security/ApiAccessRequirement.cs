using Microsoft.AspNetCore.Authorization;

namespace Piraeus.WebApi.Security
{
    public class ApiAccessRequirement : IAuthorizationRequirement
    {
        public ApiAccessRequirement(Capl.Authorization.AuthorizationPolicy policy)
        {
            Policy = policy;
        }

        public Capl.Authorization.AuthorizationPolicy Policy { get; private set; }
    }
}