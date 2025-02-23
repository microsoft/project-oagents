namespace SupportCenter.ApiService.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Orleans;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService;
using SupportCenter.ApiService.RealTimeAudio;

public class SupportCenterHub(IClusterClient clusterClient, IRealTimeAudioService audioService, ISignalRService signalRService) : Hub<ISupportCenterHub>
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

    /* Audio */
    public async Task StartVoiceInteraction(string userId, string conversationId)
    {
        string connectionId = Context.ConnectionId;

        // Use Orleans to store the active audio session.
        var voiceSessionStore = clusterClient.GetGrain<IStoreConnections>(userId);
        await voiceSessionStore.AddConnection(new Connection
        {
            Id = connectionId,
            ConversationId = conversationId
        });

        await signalRService.SendAsync(connectionId, "VoiceInteractionReady");
    }

    public async Task ProcessVoice(string userId, string conversationId, byte[] audioData)
    {
        // 1. Transcribe audio to text
        var transcribedText = await audioService.TranscribeAudioAsync(audioData);

        // 2. Create a chat message from transcribed text
        var message = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            UserId = userId,
            Text = transcribedText,
            Sender = "User"
        };

        // 3. Process through regular chat flow
        await ProcessMessage(message);

        // 4. Send transcription back to client
        //await Clients.Caller.SendAsync("ReceiveTranscription", message);
    }

    public async Task EndVoiceInteraction(string userId)
    {
        // Remove the audio session from Orleans
        var audioSessionStore = clusterClient.GetGrain<IStoreConnections>(userId);
        await audioSessionStore.RemoveConnection();
    }
}