namespace SupportCenter.ApiService.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Orleans;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService;

public class SupportCenterHub(IClusterClient clusterClient) : Hub<ISupportCenterHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var registry = clusterClient.GetGrain<IStoreConnections>(Context.UserIdentifier);
        await registry.RemoveConnection();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task ConnectToAgent(string userId, string conversationId)
    {
        var registry = clusterClient.GetGrain<IStoreConnections>(userId);
        await registry.AddConnection(new Connection { Id = Context.ConnectionId, ConversationId = conversationId });

        // Notify the agents that a new user got connected.
        var streamProvider = clusterClient.GetStreamProvider(Consts.OrleansStreamProvider);
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

    public async Task RestartConversation(string userId, string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));

        var registry = clusterClient.GetGrain<IStoreConnections>(userId);
        var connection = await registry.GetConnection();
        if (connection != null)
        {
            await registry.RemoveConnection();
        }

        var newConversationId = connection != null? connection.ConversationId : conversationId;
        await registry.AddConnection(new Connection { Id = Context.ConnectionId, ConversationId = newConversationId });

        var streamProvider = clusterClient.GetStreamProvider(Consts.OrleansStreamProvider);
        var streamId = StreamId.Create(Consts.OrleansNamespace, $"{userId}/{newConversationId}");
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
    public async Task ProcessMessage(ChatMessage chatMessage)
    {
        ArgumentNullException.ThrowIfNull(chatMessage, nameof(chatMessage));
        ArgumentException.ThrowIfNullOrWhiteSpace(chatMessage.UserId, nameof(chatMessage.UserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(chatMessage.ConversationId, nameof(chatMessage.ConversationId));

        var userId = chatMessage.UserId;
        var conversationId = chatMessage.ConversationId;

        var streamProvider = clusterClient.GetStreamProvider(Consts.OrleansStreamProvider);
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