using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Data.CosmosDb.Configurations
{
    public class CosmosDbContainerConfiguration
    {
        [Required]
        public string? DatabaseName { get; set; }

        [Required]
        public string? ContainerName { get; set; }

        [Required]
        public string? PartitionKey { get; set; }

        [Required]
        public string? EntityName { get; set; }
    }
}