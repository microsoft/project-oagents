using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Data.CosmosDb.Configurations
{
    public class CosmosDbConfiguration
    {
        [Required]
        public string? AccountUri { get; set; }

        [Required]
        public string? AccountKey { get; set; }

        [Required]
        public IEnumerable<CosmosDbContainerConfiguration>? Containers { get; set; }
    }
}
