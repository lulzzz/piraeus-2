using Orleans;
using Orleans.Providers;
using Piraeus.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class SigmaAlgebra : Grain<SigmaAlgebraState>, ISigmaAlgebra
    {
        public async Task AddAsync(string resourceUriString)
        {
            if (!State.Container.Contains(resourceUriString))
                State.Container.Add(resourceUriString);

            await WriteStateAsync();
        }

        public async Task ClearAsync()
        {
            await ClearStateAsync();
        }

        public async Task<bool> Contains(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            return await Task.FromResult<bool>(State.Container.Contains(resourceUriString));
        }

        public async Task<List<string>> GetListAsync()
        {
            return await Task.FromResult<List<string>>(State.Container);
        }

        public async Task<List<string>> GetListAsync(int index, int quantity)
        {
            if (index < 0)
                throw new IndexOutOfRangeException(nameof(index));

            if (quantity < 0)
                throw new IndexOutOfRangeException(nameof(quantity));

            if (index >= State.Container.Count)
            {
                return null;
            }
            else if (index + quantity >= State.Container.Count)
            {
                return await Task.FromResult<List<string>>(new List<string>(State.Container.Skip(index).Take(State.Container.Count - index)));
            }
            else
            {
                return await Task.FromResult<List<string>>(new List<string>(State.Container.Skip(index).Take(quantity)));
            }
        }

        public async Task<ListContinuationToken> GetListAsync(ListContinuationToken token)
        {
            _ = token ?? throw new ArgumentNullException(nameof(token));

            int remaining = token.Index + token.Quantity >= State.Container.Count ? 0 : State.Container.Count - (token.Index + token.Quantity);
            int index = token.Index + token.Quantity >= State.Container.Count ? State.Container.Count - 1 : token.Index + token.Quantity;
            int quantity = token.Index + token.Quantity >= State.Container.Count ? State.Container.Count - token.Quantity : token.Quantity;

            List<string> items = await GetListAsync(token.Index, token.Quantity);
            return new ListContinuationToken() { Index = index, Quantity = quantity, Remaining = remaining, Items = items };
        }

        public override Task OnActivateAsync()
        {
            State.Container ??= new List<string>();

            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        public async Task RemoveAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            State.Container.Remove(resourceUriString);
            await Task.CompletedTask;
        }
    }
}