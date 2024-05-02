using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Marketing.SignalRHub;

public interface ISignalRClient
{
    Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType);
}

public class SignalRClient : ISignalRClient
{
    private readonly IHubContext<ArticleHub> _hubContext;
    public SignalRClient(IHubContext<ArticleHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType)
    {
        var connectionId = SignalRConnectionsDB.ConnectionIdByUser[userId];
        var frontEndMessage = new FrontEndMessage()
        {
            UserId = userId,
            Message = message,
            Agent = agentType.ToString()
        };
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", frontEndMessage);
    }
}
