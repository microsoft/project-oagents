namespace SupportCenter.ApiService.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Orleans;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService;
using SupportCenter.ApiService.RealTimeAudio;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class SupportCenterHub(
    IClusterClient clusterClient, 
    IRealTimeAudioService audioService, 
    ISignalRService signalRService) : Hub<ISupportCenterHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // End any active voice sessions for this connection
        try 
        {
            await audioService.EndStreamingSessionAsync(Context.ConnectionId);
        }
        catch
        {
            // Ignore errors if no active session exists
        }

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

        // End any active voice sessions first
        try
        {
            await audioService.EndStreamingSessionAsync(Context.ConnectionId);
        }
        catch
        {
            // Ignore errors if no active session exists
        }

        var registry = clusterClient.GetGrain<IStoreConnections>(userId);
        var connection = await registry.GetConnection();
        if (connection != null)
        {
            await registry.RemoveConnection();
        }

        var newConversationId = connection?.ConversationId ?? conversationId;
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
            { "messageId", chatMessage.Id ?? Guid.NewGuid().ToString() }
        };

        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventType.UserChatInput),
            Data = data
        });
    }

    /* Voice Interaction Methods */
    public async Task StartVoiceInteraction(string userId, string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));
        
        string connectionId = Context.ConnectionId;

        try
        {
            // Use Orleans to store the active audio session
            var voiceSessionStore = clusterClient.GetGrain<IStoreConnections>(userId);
            await voiceSessionStore.AddConnection(new Connection
            {
                Id = connectionId,
                ConversationId = conversationId
            });

            // Notify agents that a voice session is beginning
            var streamProvider = clusterClient.GetStreamProvider(Consts.OrleansStreamProvider);
            var streamId = StreamId.Create(Consts.OrleansNamespace, $"{userId}/{conversationId}");
            var stream = streamProvider.GetStream<Event>(streamId);
            
            var data = new Dictionary<string, string>
            {
                { "userId", userId },
                { "message", "Voice interaction started" }
            };
            
            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventType.VoiceSessionStarted),
                Data = data
            });

            // Initialize realtime audio session
            await audioService.StartStreamingSessionAsync(connectionId);

            // Notify client that we're ready to receive audio
            await signalRService.SendAsync(connectionId, "VoiceInteractionReady");
            
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                Text = "Voice interaction started. You can speak now.",
                Sender = AgentType.Dispatcher.ToString()
            });
        }
        catch (Exception ex)
        {
            // Send error message to client
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                Text = "Error starting voice interaction. Please try again.",
                Sender = AgentType.Dispatcher.ToString()
            });
            throw;
        }
    }

    public async Task ProcessVoiceInput(string userId, string conversationId, byte[] audioData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));
        ArgumentNullException.ThrowIfNull(audioData, nameof(audioData));
        
        string connectionId = Context.ConnectionId;
        
        // Process the audio using the realtime API
        await audioService.ProcessRealtimeAudioAsync(connectionId, userId, conversationId, audioData);
    }

    public async Task EndVoiceInteraction(string userId, string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId, nameof(conversationId));
        
        string connectionId = Context.ConnectionId;
        
        try
        {
            // End the streaming session
            await audioService.EndStreamingSessionAsync(connectionId);

            // Send a message to the client
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                Text = "Voice interaction ended. Continuing with text chat.",
                Sender = AgentType.Dispatcher.ToString()
            });

            // The RemoveConnection will be handled in the audioService EndStreamingSessionAsync method
            // by publishing a VoiceSessionEnded event
        }
        catch (Exception ex)
        {
            // Send error message to client
            await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                UserId = userId,
                Text = "Error ending voice interaction. Please try again.",
                Sender = AgentType.Dispatcher.ToString()
            });
            throw;
        }
    }
}