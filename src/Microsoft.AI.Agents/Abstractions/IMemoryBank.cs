namespace Microsoft.AI.Agents.Abstractions;

public interface IMemoryBank : IAgent
{
    public Task ProcessEvent(Event item);
    public Task<MemoryItem> ExtractMemory(Event item);
    public Task<string> ConsolidateMemory();
    public Task<string> Recall();
    public Task<MemoryBankState> IntrospectMemory();

}
