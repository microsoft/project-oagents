namespace SupportCenter.ApiService.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToClientAsync(string messageId, string userId, string conversationId, string connectionId, string message, AgentType senderType);
    Task SendAsync(string connectionId, string methodName, object?[]? args = null);

}
