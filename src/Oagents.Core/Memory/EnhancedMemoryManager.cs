using Microsoft.SemanticKernel.Memory;
using Microsoft.Extensions.Logging;

namespace Microsoft.AI.Agents.Core.Memory
{
    public class EnhancedMemoryManager
    {
        private readonly ISemanticTextMemory _memory;
        private readonly ILogger<EnhancedMemoryManager> _logger;
        private readonly int _maxRetries = 3;

        public EnhancedMemoryManager(ISemanticTextMemory memory, ILogger<EnhancedMemoryManager> logger)
        {
            _memory = memory;
            _logger = logger;
        }

        public async Task<bool> StoreWithRetryAsync(string collection, string text, string id, string description = "", int attempt = 0)
        {
            try
            {
                await _memory.SaveInformationAsync(collection, text, id, description);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error storing memory on attempt {attempt + 1}");
                
                if (attempt < _maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
                    return await StoreWithRetryAsync(collection, text, id, description, attempt + 1);
                }
                
                return false;
            }
        }

        public async Task<MemoryQueryResult?> SearchMemoryAsync(string collection, string query, double minRelevance = 0.7, int limit = 5)
        {
            try
            {
                var results = _memory.SearchAsync(collection, query, limit: limit, minRelevanceScore: minRelevance);
                
                var items = new List<MemoryRecordMetadata>();
                await foreach (var item in results)
                {
                    items.Add(item.Metadata);
                }

                return new MemoryQueryResult
                {
                    Query = query,
                    Results = items,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching memory");
                return null;
            }
        }
    }

    public class MemoryQueryResult
    {
        public string Query { get; set; }
        public List<MemoryRecordMetadata> Results { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 