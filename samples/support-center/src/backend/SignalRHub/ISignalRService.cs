namespace SupportCenter.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToClient(string messageId, string userId, string message, AgentType agentType);
}
