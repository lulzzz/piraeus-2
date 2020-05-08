using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class SigmaAlgebraChain : Grain<SigmaAlgebraChainState>, ISigmaAlgebraChain
    {
        public async Task<bool> AddAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            if (State.Container.Count < 1000 && !State.Container.Contains(resourceUriString))
            {
                State.Container.Add(resourceUriString);
                return await Task.FromResult(true);
            }

            long nextId = State.Id;
            nextId++;

            ISigmaAlgebraChain nextChain = GrainFactory.GetGrain<ISigmaAlgebraChain>(nextId);
            while (await nextChain.GetCountAsync() >= 1000)
            {
                if (await nextChain.ContainsAsync(resourceUriString))
                {
                    return await Task.FromResult(false);
                }

                nextId++;
                nextChain = GrainFactory.GetGrain<ISigmaAlgebraChain>(nextId);
            }

            if (await nextChain.ContainsAsync(resourceUriString))
            {
                return await Task.FromResult(false);
            }

            bool result = await nextChain.AddAsync(resourceUriString);
            return await Task.FromResult(result);
        }

        public async Task ChainupAsync()
        {
            if (State.Container.Count == 0)
            {
                return;
            }

            long nextId = State.Id;
            nextId++;

            ISigmaAlgebraChain nextChain = GrainFactory.GetGrain<ISigmaAlgebraChain>(nextId);
            int count = State.Container.Count;
            int nextCount = await nextChain.GetCountAsync();

            if (count <= 1000 && nextCount > 0)
            {
                List<string> list = await nextChain.GetListAsync();
                int qty = 1000 - count;
                int delta = qty > list.Count ? list.Count : qty;

                for (int i = 0; i < delta; i++)
                {
                    State.Container.Add(list[i]);
                    await nextChain.RemoveAsync(list[i]);
                }

                nextCount = await nextChain.GetCountAsync();
                if (nextCount > 0)
                {
                    await nextChain.ChainupAsync();
                }
            }
        }

        public async Task ClearAsync()
        {
            await ClearStateAsync();
        }

        public async Task<bool> ContainsAsync(string resourceUriString)
        {
            return await Task.FromResult(State.Container.Contains(resourceUriString));
        }

        public async Task<int> GetCountAsync()
        {
            return await Task.FromResult(State.Container.Count);
        }

        public async Task<int> GetCountAsync(string filter)
        {
            string filterExp = filter.Replace("*", ".*");
            Regex regex = new Regex(filterExp);

            IEnumerable<string> en = State.Container.Where(a => regex.IsMatch(a));

            return await Task.FromResult(en.Count());
        }

        public async Task<long> GetIdAsync()
        {
            return await Task.FromResult(State.Id);
        }

        public async Task<List<string>> GetListAsync()
        {
            return await Task.FromResult(State.Container);
        }

        public async Task<List<string>> GetListAsync(string filter)
        {
            string filterExp = filter.Replace("*", ".*");
            Regex regex = new Regex(filterExp);

            IEnumerable<string> en = State.Container.Where(a => regex.IsMatch(a));
            return await Task.FromResult(new List<string>(en));
        }

        public override Task OnActivateAsync()
        {
            State.Container ??= new List<string>();
            State.Id = this.GetGrainIdentity().PrimaryKeyLong;
            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        public async Task<bool> RemoveAsync(string resourceUriString)
        {
            bool result = false;

            if (State.Container.Contains(resourceUriString))
            {
                result = State.Container.Remove(resourceUriString);
                await ChainupAsync();
                return await Task.FromResult(result);
            }

            while (!result)
            {
                long nextId = State.Id;
                nextId++;

                ISigmaAlgebraChain nextChain = GrainFactory.GetGrain<ISigmaAlgebraChain>(nextId);
                int cnt = await nextChain.GetCountAsync();

                if (cnt == 0)
                {
                    break;
                }

                result = await nextChain.RemoveAsync(resourceUriString);
            }

            if (result)
            {
                await ChainupAsync();
            }

            return await Task.FromResult(result);
        }
    }
}