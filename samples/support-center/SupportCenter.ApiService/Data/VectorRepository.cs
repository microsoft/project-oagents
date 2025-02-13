using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.Text;

namespace SupportCenter.ApiService.Data
{
    public class VectorRepository(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IVectorStore vectorStore)
    {
        public async ValueTask<string> AddKnowledge(string query, string index, int top=2)
        {
            var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(query);
            var collection = vectorStore.GetCollection<int, Movie>(index);

            var searchOptions = new VectorSearchOptions()
            {
                Top = top,
                VectorPropertyName = "Vector"
            };

            var results = await collection.VectorizedSearchAsync(queryEmbedding, searchOptions);
            var kbStringBuilder = new StringBuilder();
            //await foreach (var result in results.Results)
            //{
            //    kbStringBuilder.AppendLine($"{doc.Metadata.Text}");
            //}
            //arguments[index] = instruction.Replace($"!{index}!", $"{kbStringBuilder}");
            return "";
        }
    }
}


public class Movie
{
    [VectorStoreRecordKey]
    public int Key { get; set; }

    [VectorStoreRecordData]
    public string Title { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}