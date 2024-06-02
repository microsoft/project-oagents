using Microsoft.AspNetCore.SignalR;

namespace SupportCenter.SignalRHub;

public class SignalRService(IHubContext<SupportCenterHub> hubContext) : ISignalRService
{
    public async Task SendMessageToSpecificClient(string id, string userId, string message, AgentType agentType)
    {
        var connectionId = SignalRConnectionsDB.ConnectionIdByUser[userId].ConnectionId ?? throw new Exception("Cannot find connection Id");
        var chatMessage = new ChatMessage()
        {
            Id = id,
            UserId = userId,
            Text = message,
            Sender = agentType.ToString()
        };
        await hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", chatMessage);
    }
}
