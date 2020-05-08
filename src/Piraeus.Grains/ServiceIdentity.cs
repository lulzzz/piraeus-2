using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class ServiceIdentity : Grain<ServiceIdentityState>, IServiceIdentity
    {
        public Task AddCertificateAsync(byte[] certificate)
        {
            State.Certificate = certificate;
            return WriteStateAsync();
        }

        public async Task AddClaimsAsync(List<KeyValuePair<string, string>> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                return;
            }

            State.Claims = new List<KeyValuePair<string, string>>();
            foreach (var claim in claims)
                State.Claims.Add(new KeyValuePair<string, string>(claim.Key, claim.Value));

            await Task.CompletedTask;
        }

        public async Task<byte[]> GetCertificateAsync()
        {
            return await Task.FromResult(State.Certificate);
        }

        public async Task<List<KeyValuePair<string, string>>> GetClaimsAsync()
        {
            return await Task.FromResult(State.Claims);
        }

        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }
    }
}