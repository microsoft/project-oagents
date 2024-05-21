namespace SupportCenter.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType);
}
