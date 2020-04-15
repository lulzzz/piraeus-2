using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Piraeus.GrainInterfaces
{
    public interface ISigmaAlgebra : IGrainWithStringKey
    {
        Task<bool> AddAsync(string resourceUriString);

        Task ClearAsync();

        Task<bool> ContainsAsync(string resourceUriString);

        Task<int> GetCountAsync();

        Task<List<string>> GetListAsync();

        Task<List<string>> GetListAsync(string filter);

        Task<List<string>> GetListAsync(int index, int pageSize);

        Task<List<string>> GetListAsync(int index, int pageSize, string filter);

        Task<ListContinuationToken> GetListAsync(ListContinuationToken token);

        Task<ListContinuationToken> GetListAsync(ListContinuationToken token, string filter);


        Task RemoveAsync(string resourceUriString);
    }
}