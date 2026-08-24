namespace Microsoft.AI.Agents.Abstractions;

public class MemoryBankState
{
    public List<MemoryItem> Memories { get; set; }
    public List<Event> Events { get; set; }
    public string Summary { get; set; }
}