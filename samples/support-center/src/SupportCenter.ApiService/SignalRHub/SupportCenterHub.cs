namespace SupportCenter.ApiService.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Orleans;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Options;

public class SupportCenterHub : Hub<ISupportCenterHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        SignalRConnectionsDB.ConnectionByUser.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ConnectToAgent(string userId, string conversationId, IClusterClient clusterClient)
    {
        SignalRConnectionsDB.ConnectionByUser.AddOrUpdate(
            userId, new Connection(Context.ConnectionId, conversationId),
            (key, oldValue) => new Connection(Context.ConnectionId, conversationId));

        // Notify the agents that a new user got connected.
        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, $"{userId}/{conversationId}");
        var stream = streamProvider.GetStream<Event>(streamId);
        var data = new Dictionary<string, string>
        {
            { nameof(userId), userId },
            { "userMessage", "Connected to Agents"},
        };
        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventType.UserConnected),
            Data = data
        });
    }

    public async Task RestartConversation(string userId, string conversationId, IClusterClient clusterClient)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));

        string? oldConversationId = SignalRConnectionsDB.GetConversationId(userId);

        SignalRConnectionsDB.ConnectionByUser.AddOrUpdate(
            userId,
            key => new Connection(Context.ConnectionId, conversationId),
            (key, oldValue) => new Connection(oldValue.Id, conversationId));

        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, $"{userId}/{conversationId}");
        var stream = streamProvider.GetStream<Event>(streamId);
        await stream.OnNextAsync(
            new Event
            {
                Type = nameof(EventType.UserNewConversation),
                Data = new Dictionary<string, string> { { "userId", userId }, { "userMessage", "Conversation restarted." } }
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
        ArgumentNullException.ThrowIfNull(chatMessage, nameof(chatMessage));
        ArgumentException.ThrowIfNullOrWhiteSpace(chatMessage.UserId, nameof(chatMessage.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(chatMessage.ConversationId, nameof(chatMessage.ConversationId));

        var userId = chatMessage.UserId;
        var conversationId = chatMessage.ConversationId;

        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, $"{userId}/{conversationId}");
        var stream = streamProvider.GetStream<Event>(streamId);

        var data = new Dictionary<string, string>
        {
            { "userId", chatMessage.UserId },
            { "userMessage", chatMessage.Text ?? string.Empty },
        };

        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventType.UserChatInput),
            Data = data
        });
    }
}