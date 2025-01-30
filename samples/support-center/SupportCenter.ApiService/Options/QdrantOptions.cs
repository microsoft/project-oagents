using System.ComponentModel.DataAnnotations;

namespace SupportCenter.ApiService.Options;
public class QdrantOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public int VectorSize { get; set; }
}