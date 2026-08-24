namespace Microsoft.AI.Agents.Abstractions;

public class MemoryItem
{
    public Guid Id { get; set; }
    public string Memory { get; set; }
    public List<string> Tags { get; set; }
}
