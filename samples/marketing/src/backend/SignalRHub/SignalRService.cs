﻿using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Marketing.SignalRHub;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<ArticleHub> _hubContext;
    public SignalRService(IHubContext<ArticleHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendMessageToSpecificClient(string sessionId, string message, AgentTypes agentType)
    {
        var connectionId = SignalRConnectionsDB.ConnectionIdByUser[sessionId];
        var frontEndMessage = new FrontEndMessage()
        {
            SessionId = sessionId,
            Message = message,
            Agent = agentType.ToString()
        };
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", frontEndMessage);
    }
}
