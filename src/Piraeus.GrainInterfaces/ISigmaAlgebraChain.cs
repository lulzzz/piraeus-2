using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piraeus.GrainInterfaces
{
    public interface ISigmaAlgebraChain : IGrainWithIntegerKey
    {
        Task ClearAsync();
        Task<int> GetCountAsync();

        Task<int> GetCountAsync(string filter);

        Task<long> GetIdAsync();

        Task ChainupAsync();

        Task<bool> ContainsAsync(string resourceUriString);

        Task<List<string>> GetListAsync();

        Task<List<string>> GetListAsync(string filter);

        Task<bool> AddAsync(string resourceUriString);

        Task<bool> RemoveAsync(string resourceUriString);


    }
}
