namespace Microsoft.AI.Agents.Abstractions;

public class AgentState<T> where T: class, new()
{
    public List<ChatHistoryItem> History { get; set; } = new();
    public T Data { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string CurrentStatus { get; set; } = "Ready";
    
    public void UpdateState(T newData)
    {
        Data = newData;
        LastUpdated = DateTime.UtcNow;
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
    }
}
