using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Options
{
    public class CosmosDbOptions
    {
        public string? AccountUri { get; set; }

        public string? AccountKey { get; set; }

        public IEnumerable<CosmosDbContainerOptions>? Containers { get; set; }
    }
}
