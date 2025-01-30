using Newtonsoft.Json;

namespace SupportCenter.ApiService.Data.Entities
{
    public abstract class Entity
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("_etag")]
        public string? ETag { get; }

        public DateTimeOffset LastUpdatedTime { get; set; }

        public abstract string GetPartitionKeyValue();
    }
}
