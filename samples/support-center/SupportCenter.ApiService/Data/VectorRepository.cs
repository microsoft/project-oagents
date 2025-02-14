using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SupportCenter.Shared;

namespace SupportCenter.ApiService.Data
{
    public class VectorRepository(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IVectorStore vectorStore) : IVectorRepository
    {
        public async Task<IAsyncEnumerable<VectorSearchResult<Document>>> GetDocuments(string query, string index, int top=2)
        {
            var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(query);
            var collection = vectorStore.GetCollection<string, Document>(index);

            var searchOptions = new VectorSearchOptions()
            {
                Top = top,
                VectorPropertyName = "Vector"
            };

            var results = await collection.VectorizedSearchAsync(queryEmbedding, searchOptions);
            return results.Results;
        }
    }
}