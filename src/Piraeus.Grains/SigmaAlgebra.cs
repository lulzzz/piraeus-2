using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using Piraeus.Core.Logging;
using Piraeus.Core.Messaging;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class SigmaAlgebra : Grain<SigmaAlgebraState>, ISigmaAlgebra
    {
        [NonSerialized] private readonly ILog logger;

        public SigmaAlgebra(ILog logger = null)
        {
            this.logger = logger;
        }

        public async Task<bool> AddAsync(string resourceUriString)
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            bool result = await chain.AddAsync(resourceUriString);
            await logger?.LogInformationAsync($"SigmaAlgebra add '{result}' for '{resourceUriString}'");
            return await Task.FromResult(result);
        }

        public async Task ClearAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int cnt = await chain.GetCountAsync();

            while (cnt > 0)
            {
                await chain.ClearAsync();
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            await logger?.LogInformationAsync("SigmaAlgebra cleared.");
            await ClearStateAsync();
        }

        public async Task<bool> ContainsAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            if (await chain.ContainsAsync(resourceUriString))
            {
                return await Task.FromResult(true);
            }

            int cnt = await chain.GetCountAsync();

            while (cnt > 0)
            {
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                if (await chain.ContainsAsync(resourceUriString))
                {
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }

        public async Task<int> GetCountAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int cnt = await chain.GetCountAsync();
            int total = cnt;

            while (cnt > 0)
            {
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
                total += cnt;
            }

            return await Task.FromResult(total);
        }

        public async Task<List<string>> GetListAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
            {
                return await Task.FromResult(new List<string>());
            }

            List<string> list = new List<string>();

            while (cnt > 0)
            {
                list.AddRange(await chain.GetListAsync());
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            list.Sort();
            return await Task.FromResult(list);
        }

        public async Task<List<string>> GetListAsync(string filter)
        {
            _ = filter ?? throw new ArgumentNullException(nameof(filter));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
            {
                return await Task.FromResult(new List<string>());
            }

            List<string> list = new List<string>();

            while (cnt > 0)
            {
                list.AddRange(await chain.GetListAsync(filter));
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            list.Sort();
            return await Task.FromResult(list);
        }

        public async Task<List<string>> GetListAsync(int index, int pageSize)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException(nameof(pageSize));
            }

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
            {
                return await Task.FromResult(new List<string>());
            }

            List<string> list = new List<string>();
            int numItems = 0;

            while (cnt > 0)
            {
                numItems += cnt;
                if (index > numItems)
                {
                    id++;
                    chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                    cnt = await chain.GetCountAsync();
                }
                else
                {
                    int stdIndex = index - (Convert.ToInt32(id) - 1) * 1000;
                    List<string> chainList = await chain.GetListAsync();

                    if (pageSize <= cnt - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(pageSize));
                        return await Task.FromResult(list);
                    }

                    if (pageSize > cnt - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(cnt - index));
                        pageSize -= cnt - index;
                    }
                }
            }

            return await Task.FromResult(list);
        }

        public async Task<List<string>> GetListAsync(int index, int pageSize, string filter)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (pageSize < 0)
            {
                throw new IndexOutOfRangeException(nameof(pageSize));
            }

            _ = filter ?? throw new ArgumentNullException(nameof(filter));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();

            if (cnt == 0)
            {
                return await Task.FromResult(new List<string>());
            }

            List<string> list = new List<string>();

            while (cnt > 0)
            {
                int filterCount = +await chain.GetCountAsync(filter);

                if (index > filterCount)
                {
                    id++;
                    chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                    cnt = await chain.GetCountAsync();
                }
                else
                {
                    int stdIndex = index - (Convert.ToInt32(id) - 1) * 1000;
                    List<string> chainList = await chain.GetListAsync(filter);

                    if (pageSize <= filterCount - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(pageSize));
                        return await Task.FromResult(list);
                    }

                    if (pageSize > filterCount - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(filterCount - index));
                        pageSize -= filterCount - index;
                    }
                }
            }

            return await Task.FromResult(list);
        }

        public async Task<ListContinuationToken> GetListAsync(ListContinuationToken token)
        {
            _ = token ?? throw new ArgumentNullException(nameof(token));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            _ = token.Filter != null ? await chain.GetCountAsync(token.Filter) : await chain.GetCountAsync();

            if (token.Filter != null)
            {
                List<string> filterItems = await GetListAsync(token.Index, token.PageSize, token.Filter);
                return await Task.FromResult(new ListContinuationToken(token.Index + filterItems.Count, token.Quantity,
                    token.PageSize, token.Filter, filterItems));
            }

            List<string> items = await GetListAsync(token.Index, token.PageSize);
            return await Task.FromResult(new ListContinuationToken(token.Index + items.Count, token.Quantity,
                token.PageSize, items));
        }

        public override Task OnActivateAsync()
        {
            return Task.CompletedTask;
        }

        public override async Task OnDeactivateAsync()
        {
            await WriteStateAsync();
        }

        public async Task RemoveAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            bool result = await chain.RemoveAsync(resourceUriString);
            await logger?.LogInformationAsync($"SigmaAlgebra removed {result} on {resourceUriString}.");
            await Task.FromResult(result);
        }
    }
}