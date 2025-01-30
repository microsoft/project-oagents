using Microsoft.AspNetCore.SignalR;

namespace SupportCenter.ApiService.SignalRHub;

public class SignalRService(IHubContext<SupportCenterHub> hubContext) : ISignalRService
{
    public async Task SendMessageToClient(string messageId, string userId, string message, AgentType senderType)
    {
        var connection = SignalRConnectionsDB.ConnectionByUser[userId];
        if (connection == null || connection.Id == null)
        {
            return;
        }

        var chatMessage = new ChatMessage()
        {
            Id = messageId,
            ConversationId = connection.ConversationId,
            UserId = userId,
            Text = message,
            Sender = senderType.ToString()
        };
        await hubContext.Clients.Client(connection.Id).SendAsync("ReceiveMessage", chatMessage);
    }
}
