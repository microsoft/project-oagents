namespace Microsoft.AI.Agents.Abstractions;

public interface IAgent<T> where T : class, new()
{
    Task HandleEvent(Event item);
    Task PublishEvent(string ns, string id, Event item);
    Task<AgentState<T>> GetStateAsync();
    Task<bool> UpdateStateAsync(AgentState<T> state);
    Task<bool> ProcessMessageAsync(string message);
    Task<bool> ResetStateAsync();
}