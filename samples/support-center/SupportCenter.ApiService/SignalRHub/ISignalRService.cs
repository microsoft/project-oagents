namespace SupportCenter.ApiService.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToClient(string messageId, string userId, string conversationId, string connectionId, string message, AgentType senderType);
}
