using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Options;
public class CosmosDbOptions
{
    [Required]
    public string? ConnectionString { get; set; }
}