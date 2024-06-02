namespace SupportCenter.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToSpecificClient(string id, string userId, string message, AgentType agentType);
}
