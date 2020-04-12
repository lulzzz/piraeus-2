using Capl.Authorization;
using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;

namespace Piraeus.GrainInterfaces
{
    public interface IAccessControl : IGrainWithStringKey
    {
        Task ClearAsync();

        [AlwaysInterleave]
        Task<AuthorizationPolicy> GetPolicyAsync();

        [AlwaysInterleave]
        Task UpsertPolicyAsync(AuthorizationPolicy policy);
    }
}