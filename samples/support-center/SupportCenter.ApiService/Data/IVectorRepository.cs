using Microsoft.Extensions.VectorData;
using SupportCenter.Shared;

namespace SupportCenter.ApiService.Data
{
    public interface IVectorRepository
    {
        Task<IAsyncEnumerable<VectorSearchResult<Document>>> GetDocuments(string query, string index, int top = 2);
    }
}
