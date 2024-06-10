using System.ComponentModel.DataAnnotations;

namespace SupportCenter.Options;
public class QdrantOptions
{
    [Required]
    public string Endpoint { get; set; } = string.Empty;
    [Required]
    public int VectorSize { get; set; }
}