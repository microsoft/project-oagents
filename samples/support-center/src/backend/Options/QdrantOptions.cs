using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Options;
public class QdrantOptions
{
    [Required]
    public string? Endpoint { get; set; }
    [Required]
    public int VectorSize { get; set; }
}