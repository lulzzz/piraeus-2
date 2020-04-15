using Orleans;
using Orleans.Providers;
using Piraeus.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piraeus.Grains
{
    [StorageProvider(ProviderName = "store")]
    [Serializable]
    public class SigmaAlgebra : Grain<SigmaAlgebraState>, ISigmaAlgebra
    {
        public async Task<bool> AddAsync(string resourceUriString)
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            return await chain.AddAsync(resourceUriString);
        }

        public async Task ClearAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int cnt = await chain.GetCountAsync();

            while(cnt > 0)
            {
                await chain.ClearAsync();
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            await ClearStateAsync();
        }

        public async Task<bool> ContainsAsync(string resourceUriString)
        {
            _ = resourceUriString ?? throw new ArgumentNullException(nameof(resourceUriString));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            if (await chain.ContainsAsync(resourceUriString))
                return await Task.FromResult<bool>(true);

            int cnt = await chain.GetCountAsync();

            while(cnt > 0)
            {
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                if (await chain.ContainsAsync(resourceUriString))
                    return await Task.FromResult<bool>(true);
            }

            return await Task.FromResult<bool>(false);
        }

        public async Task<int> GetCountAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int cnt = await chain.GetCountAsync();
            int total = cnt;

            while(cnt > 0)
            {
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
                total += cnt;
            }

            return await Task.FromResult<int>(total);
        }

        public async Task<List<string>> GetListAsync()
        {
            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
                return await Task.FromResult<List<string>>(new List<string>());

            List<string> list = new List<string>();

            while (cnt > 0)
            {
                list.AddRange(await chain.GetListAsync());
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            list.Sort();
            return await Task.FromResult<List<string>>(list);
        }

        public async Task<List<string>> GetListAsync(string filter)
        {
            _ = filter ?? throw new ArgumentNullException(nameof(filter));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
                return await Task.FromResult<List<string>>(new List<string>());

            List<string> list = new List<string>();

            while(cnt > 0)
            {
                list.AddRange(await chain.GetListAsync(filter));
                id++;
                chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                cnt = await chain.GetCountAsync();
            }

            list.Sort();
            return await Task.FromResult<List<string>>(list);
        }

        public async Task<List<string>> GetListAsync(int index, int pageSize)
        {
            if (index < 0)
                throw new IndexOutOfRangeException(nameof(index));

            if (pageSize < 0)
                throw new IndexOutOfRangeException(nameof(pageSize));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();
            if (cnt == 0)
                return await Task.FromResult<List<string>>(new List<string>());

            List<string> list = new List<string>();
            int numItems = 0;

            while (cnt > 0)
            {
                numItems += cnt;
                if(index > numItems)
                {
                    id++;
                    chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                    cnt = await chain.GetCountAsync();
                    continue;
                }
                else
                {
                    int stdIndex = index - ((Convert.ToInt32(id) - 1) * 1000);
                    List<string> chainList = await chain.GetListAsync();

                    if (pageSize <= cnt - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(pageSize));
                        return await Task.FromResult<List<string>>(list);
                    }
                    else if (pageSize > cnt - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(cnt - index));
                        pageSize -= (cnt - index);
                    }
                }
            }

            return await Task.FromResult<List<string>>(list);
        }

        public async Task<List<string>> GetListAsync(int index, int pageSize, string filter)
        {
            if (index < 0)
                throw new IndexOutOfRangeException(nameof(index));

            if (pageSize < 0)
                throw new IndexOutOfRangeException(nameof(pageSize));

            _ = filter ?? throw new ArgumentNullException(nameof(filter));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);

            int cnt = await chain.GetCountAsync();

            if (cnt == 0)
                return await Task.FromResult<List<string>>(new List<string>());

            List<string> list = new List<string>();

            while (cnt > 0)
            {
                int filterCount =+ await chain.GetCountAsync(filter);

                if (index > filterCount)
                {
                    id++;
                    chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
                    cnt = await chain.GetCountAsync();
                    continue;
                }
                else
                {
                    int stdIndex = index - ((Convert.ToInt32(id) - 1) * 1000);
                    List<string> chainList = await chain.GetListAsync(filter);

                    if (pageSize <= filterCount - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(pageSize));
                        return await Task.FromResult<List<string>>(list);
                    }
                    else if (pageSize > filterCount - index)
                    {
                        list.AddRange(chainList.Skip(stdIndex).Take(filterCount - index));
                        pageSize -= (filterCount - index);
                    }
                }
            }

            return await Task.FromResult<List<string>>(list);
        }

        public async Task<ListContinuationToken> GetListAsync(ListContinuationToken token)
        {
            _ = token ?? throw new ArgumentNullException(nameof(token));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int count = token.Filter != null ? await chain.GetCountAsync(token.Filter) : await chain.GetCountAsync();

            if(token.Filter != null)
            {
                List<string> filterItems = await GetListAsync(token.Index, token.PageSize, token.Filter);
                return await Task.FromResult<ListContinuationToken>(new ListContinuationToken(token.Index + filterItems.Count, token.Quantity, token.PageSize, token.Filter, filterItems));
            }
            else
            {
                List<string> items = await GetListAsync(token.Index, token.PageSize);
                return await Task.FromResult<ListContinuationToken>(new ListContinuationToken(token.Index + items.Count, token.Quantity, token.PageSize, items));
            }
        }

        public async Task<ListContinuationToken> GetListAsync(ListContinuationToken token, string filter)
        {
            _ = token ?? throw new ArgumentNullException(nameof(token));
            _ = filter ?? throw new ArgumentNullException(nameof(filter));

            long id = 1;
            ISigmaAlgebraChain chain = GrainFactory.GetGrain<ISigmaAlgebraChain>(id);
            int count = await chain.GetCountAsync(filter);

            int remaining = token.Index + token.Quantity >= State.Container.Count ? 0 : State.Container.Count - (token.Index + token.Quantity);
            int index = token.Index + token.Quantity >= State.Container.Count ? State.Container.Count - 1 : token.Index + token.Quantity;
            int quantity = token.Index + token.Quantity >= State.Container.Count ? State.Container.Count - token.Quantity : token.Quantity;

            List<string> items = await GetListAsync(token.Index, token.Quantity, filter);
            return new ListContinuationToken() { Index = index, Quantity = quantity, PageSize = remaining, Items = items };



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