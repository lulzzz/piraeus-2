using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace Piraeus.GrainInterfaces
{
    public interface ISigmaAlgebraChain : IGrainWithIntegerKey
    {
        Task<bool> AddAsync(string resourceUriString);

        Task ChainupAsync();

        Task ClearAsync();

        Task<bool> ContainsAsync(string resourceUriString);

        Task<int> GetCountAsync();

        Task<int> GetCountAsync(string filter);

        Task<long> GetIdAsync();

        Task<List<string>> GetListAsync();

        Task<List<string>> GetListAsync(string filter);

        Task<bool> RemoveAsync(string resourceUriString);
    }
}