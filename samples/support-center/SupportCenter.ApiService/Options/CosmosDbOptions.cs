using System.ComponentModel.DataAnnotations;

namespace SupportCenter.ApiService.Options
{
    public class CosmosDbOptions
    {
        public string? AccountUri { get; set; }

        public string? AccountKey { get; set; }

        public IEnumerable<CosmosDbContainerOptions>? Containers { get; set; }
    }
}
