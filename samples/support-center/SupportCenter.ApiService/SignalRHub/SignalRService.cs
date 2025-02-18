using Microsoft.AspNetCore.SignalR;

namespace SupportCenter.ApiService.SignalRHub;

public class SignalRService(IHubContext<SupportCenterHub> hubContext) : ISignalRService
{
    public async Task SendMessageToClient(string messageId, string userId, string conversationId, string connectionId, string message, AgentType senderType)
    {
        var chatMessage = new ChatMessage()
        {
            Id = messageId,
            ConversationId = conversationId,
            UserId = userId,
            Text = message,
            Sender = senderType.ToString()
        };
        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", chatMessage);
    }
}
