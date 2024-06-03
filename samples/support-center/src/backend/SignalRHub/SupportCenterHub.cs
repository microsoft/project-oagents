namespace SupportCenter.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using SupportCenter.Options;
using SupportCenter.Events;

public class SupportCenterHub : Hub<ISupportCenterHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();        
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        SignalRConnectionsDB.ConnectionIdByUser.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ConnectToAgent(string userId, string conversationId, IClusterClient clusterClient)
    {
        var chatMessage = new ChatMessage()
        {
            UserId = userId,
            Text = "Connected to Agents",
            Sender = AgentType.Chat.ToString()
        };

        SignalRConnectionsDB.ConnectionIdByUser.AddOrUpdate(
            userId, new Connection(Context.ConnectionId, conversationId), 
            (key, oldValue) => new Connection(Context.ConnectionId, conversationId));

        // Notify the agents that a new user got connected.
        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, chatMessage.UserId);
        var stream = streamProvider.GetStream<Event>(streamId);
        var data = new Dictionary<string, string>
        {
            { "userId", chatMessage.UserId },
            { "userMessage", chatMessage.Text},
        };
        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventType.UserConnected),
            Data = data
        });
    }

    /// <summary>
    /// This method is called when a new message from the client arrives.
    /// </summary>
    /// <param name="chatMessage"></param>
    /// <param name="clusterClient"></param>
    /// <returns></returns>
    public async Task ProcessMessage(ChatMessage chatMessage, IClusterClient clusterClient)
    {
        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, chatMessage.UserId);
        var stream = streamProvider.GetStream<Event>(streamId);

        var data = new Dictionary<string, string>
        {
            { "userId", chatMessage.UserId },
            { "userMessage", chatMessage.Text},
        };

        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventType.UserChatInput),
            Data = data
        });
    }
}