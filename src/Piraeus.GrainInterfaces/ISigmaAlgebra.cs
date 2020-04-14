using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piraeus.GrainInterfaces
{
    public interface ISigmaAlgebra : IGrainWithStringKey
    {
        Task AddAsync(string resourceUriString);

        Task ClearAsync();

        Task<bool> Contains(string resourceUriString);

        Task<List<string>> GetListAsync();

        Task<List<string>> GetListAsync(int index, int quantity);

        Task<ListContinuationToken> GetListAsync(ListContinuationToken token);

        Task RemoveAsync(string resourceUriString);
    }
}