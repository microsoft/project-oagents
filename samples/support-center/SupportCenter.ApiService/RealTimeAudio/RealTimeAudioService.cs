using Azure;
using Microsoft.AI.Agents.Abstractions;
using OpenAI;
using OpenAI.Audio;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.SignalRHub;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.RealTimeAudio;

public class RealTimeAudioService : IRealTimeAudioService
{
    private readonly string _realtimeDeploymentName = "gpt-4o"; // Use the appropriate deployment name
    private readonly ConcurrentDictionary<string, RealtimeSession> _activeSessions = new();
    private readonly GeneratedSpeechVoice _speechVoice = GeneratedSpeechVoice.Alloy;
    // Maximum time a session can be inactive before being closed (5 minutes)
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(5);
    private readonly ILogger<RealTimeAudioService> logger;
    private readonly ISignalRService signalRService;
    private readonly OpenAIClient openAIClient;
    private readonly AudioClient audioClient;
    private readonly IClusterClient clusterClient;

    private static readonly string[] agentTypes = ["Conversation", "QnA", "CustomerInfo", "Invoice", "Dispatcher"];
    private static readonly string[] eventTypes = ["UserChatInput", "ConversationRequested", "QnARequested", "CustomerInfoRequested", "InvoiceRequested"];

    public RealTimeAudioService(
        ILogger<RealTimeAudioService> logger,
        ISignalRService signalRService,
        [FromKeyedServices(Gpt4oMini)] OpenAIClient openAIClient,
        [FromKeyedServices(Whisper)] AudioClient audioClient,
        IClusterClient clusterClient)
    {
        this.logger = logger;
        this.signalRService = signalRService;
        this.openAIClient = openAIClient;
        this.audioClient = audioClient;
        this.clusterClient = clusterClient;

        // Start a background task to check for idle sessions
        _ = Task.Run(CleanupIdleSessionsAsync);
    }

    private async Task CleanupIdleSessionsAsync()
    {
        while (true)
        {
            try
            {
                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1));

                // Get all sessions that have been inactive for too long
                var now = DateTimeOffset.UtcNow;
                var timeoutSessions = _activeSessions
                    .Where(kvp => (now - kvp.Value.LastActivity) > _sessionTimeout)
                    .ToList();

                foreach (var session in timeoutSessions)
                {
                    logger.LogInformation("Closing idle session {ConnectionId} after {Timeout} of inactivity",
                        session.Key, _sessionTimeout);
                    await EndStreamingSessionAsync(session.Key);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up idle sessions");
                // Keep running even if there's an error
            }
        }
    }

    public async Task<string> TranscribeAudioAsync(byte[] audioData)
    {
        ArgumentNullException.ThrowIfNull(audioData, nameof(audioData));

        try
        {
            using var audioStream = new MemoryStream(audioData, writable: false);

            // Note: This method still uses the existing transcription for non-realtime scenarios
            var response = await audioClient.TranscribeAudioAsync(audioStream, "audio");
            return response.Value.Text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transcribing audio of size {Size} bytes", audioData.Length);
            throw;
        }
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        try
        {
            logger.LogInformation("Synthesizing speech for text: {TextLength} characters", text.Length);

            // Use the same voice as configured for the realtime session for consistency
            var options = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Wav,
                SpeedRatio = 1.0f,
            };

            // Implement retry logic with exponential backoff for transient failures
            int maxRetries = 3;
            int retryDelayMs = 200;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await audioClient.GenerateSpeechAsync(text, _speechVoice, options);

                    // Copy the result to a byte array
                    using var memoryStream = new MemoryStream(result.Value.ToArray());
                    return memoryStream.ToArray();
                }
                catch (Exception ex) when (attempt < maxRetries &&
                    (ex is RequestFailedException rfe && (rfe.Status == 429 || rfe.Status >= 500) ||
                     ex is TimeoutException))
                {
                    // Exponential backoff for retry
                    await Task.Delay(retryDelayMs * (int)Math.Pow(2, attempt));
                    logger.LogWarning(ex, "Transient error synthesizing speech, attempt {Attempt}/{MaxRetries}",
                        attempt + 1, maxRetries);
                }
            }
            throw new Exception("Failed to synthesize speech after multiple attempts");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error synthesizing speech with text length: {TextLength}", text.Length);
            throw;
        }
    }

    public async Task StartStreamingSessionAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);

        try
        {
            // Create a new session for realtime streaming with cancelation token
            var session = new RealtimeSession(connectionId);
            _activeSessions[connectionId] = session;

            // Start a background task to handle the GPT-4o Realtime API streaming
            _ = Task.Run(() => HandleRealtimeSessionAsync(session));

            logger.LogInformation("Started realtime audio session for connection {ConnectionId}", connectionId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting streaming session");
            throw;
        }
    }

    private async Task HandleRealtimeSessionAsync(RealtimeSession session)
    {
        try
        {
            // Define functions for GPT-4o to call
            var routingFunction = new FunctionDefinition()
            {
                Name = "route_user_query",
                Description = "Routes user query to the appropriate agent based on intent",
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        agentType = new
                        {
                            type = "string",
                            @enum = agentTypes,
                            description = "The type of agent to handle this query"
                        },
                        userIntent = new
                        {
                            type = "string",
                            description = "A brief description of what the user is trying to do"
                        },
                        eventType = new
                        {
                            type = "string",
                            @enum = eventTypes,
                            description = "The specific event type to trigger in the Orleans event system"
                        }
                    },
                    required = new[] { "agentType", "userIntent", "eventType" }
                })
            };

            var functionTool = new ChatCompletionsFunctionToolDefinition(routingFunction);
            var tools = new List<ChatCompletionsToolDefinition> { functionTool };

            // Set up GPT-4o Realtime API options with function calling
            var options = new AudioRealtimeSessionOptions
            {
                // Configure with appropriate settings
                DeploymentName = _realtimeDeploymentName,
                Temperature = 0.7f,
                MaxTokens = 800,
                AudioOutputFormat = AudioRealtimeOutputFormat.Wav,
                Voice = _speechVoice.ToString().ToLowerInvariant(),
                // Add system prompt to inform GPT-4o about its role in the multi-agent system
                SystemPrompt = @"You are part of a support center multi-agent system. The user is speaking with you through a voice interface. 
You will transcribe their speech, determine the user's intent, and route their query to the appropriate agent.

Available agents and their corresponding events:
- Conversation (ConversationRequested): For general conversation, greetings, and simple interactions
- QnA (QnARequested): For answering questions based on documentation and knowledge base
- CustomerInfo (CustomerInfoRequested): For updating or retrieving customer information like addresses and contact details
- Invoice (InvoiceRequested): For handling invoice-related questions and issues
- Dispatcher (UserChatInput): If you're unsure which agent should handle the query

When routing a query, you MUST call the route_user_query function with:
1. The appropriate agentType
2. A brief description of the userIntent
3. The corresponding eventType from the list above

Your verbal response should be helpful but brief, acknowledging the user and letting them know you're processing their request.
For example: 'I'll help you with that invoice question. Let me connect you with our invoice specialist.'

Respond in the same language the user speaks to you.",
                Tools = tools
            };

            // Create streaming session
            await using var sessionStream = await openAIClient.GetAudioRealtimeStreamingClientAsync(options, session.CancellationToken);
            session.IsSessionActive = true;

            // Handle audio chunks processing
            while (!session.CancellationToken.IsCancellationRequested && session.IsSessionActive)
            {
                // Try to dequeue an audio chunk
                if (session.AudioChunks.TryDequeue(out var audioChunk))
                {
                    // Send audio chunk to the API
                    await sessionStream.AudioStreamAsync(
                        BinaryData.FromBytes(audioChunk),
                        session.CancellationToken);
                }

                // Process responses from GPT-4o
                await foreach (var response in sessionStream.StreamResponsesAsync(session.CancellationToken))
                {
                    if (response.ContentUpdate != null && !string.IsNullOrEmpty(response.ContentUpdate.Text))
                    {
                        // Handle transcribed text
                        var text = response.ContentUpdate.Text;
                        session.TranscribedText.Append(text);

                        // Send partial transcription to client
                        await signalRService.SendAsync(session.ConnectionId, "ReceivePartialTranscription", text);
                    }
                    else if (response.AudioUpdate != null && response.AudioUpdate.AudioData != null)
                    {
                        // Handle audio response
                        var audioData = response.AudioUpdate.AudioData;
                        
                        // Send audio response to client
                        await signalRService.SendAsync(session.ConnectionId, "ReceiveAudioResponse", audioData.ToArray());
                    }
                    else if (response.ToolCallsUpdate != null && response.ToolCallsUpdate.ToolCalls.Any())
                    {
                        foreach (var toolCall in response.ToolCallsUpdate.ToolCalls)
                        {
                            // Handle function call to route to appropriate agent
                            if (toolCall is ChatCompletionsFunctionToolCall functionCall &&
                                functionCall.Name == "route_user_query")
                            {
                                var arguments = JsonSerializer.Deserialize<Dictionary<string, string>>(
                                    functionCall.Arguments?.ToString() ?? "{}");

                                if (arguments != null &&
                                    arguments.TryGetValue("agentType", out var agentType) &&
                                    arguments.TryGetValue("userIntent", out var userIntent) &&
                                    arguments.TryGetValue("eventType", out var eventType))
                                {
                                    string transcribedText = session.TranscribedText.ToString().Trim();

                                    if (session.UserId != null && session.ConversationId != null && !string.IsNullOrEmpty(transcribedText))
                                    {
                                        // Route to the appropriate agent based on the function call
                                        await RouteToAgentBasedOnIntent(
                                            session.UserId,
                                            session.ConversationId,
                                            transcribedText,
                                            agentType,
                                            userIntent,
                                            eventType);

                                        // Clear the buffer after publishing
                                        session.TranscribedText.Clear();
                                    }
                                }
                            }
                        }
                    }
                }

                // Small delay to prevent CPU spiking
                await Task.Delay(10, session.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Realtime session {ConnectionId} was cancelled", session.ConnectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in realtime audio session for connection {ConnectionId}", session.ConnectionId);
        }
        finally
        {
            session.IsSessionActive = false;
        }
    }

    public async Task ProcessRealtimeAudioAsync(string connectionId, string userId, string conversationId, byte[] audioData)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(conversationId);
        ArgumentNullException.ThrowIfNull(audioData);

        try
        {
            if (!_activeSessions.TryGetValue(connectionId, out var session))
            {
                logger.LogWarning("No active session for connection {ConnectionId}", connectionId);
                return;
            }

            // Store user info for Orleans integration
            session.UserId = userId;
            session.ConversationId = conversationId;

            // Store the audio data for processing by the background task
            session.AddAudioChunk(audioData);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing realtime audio for user {UserId} in conversation {ConversationId}",
                userId, conversationId);
        }
    }

    public async Task EndStreamingSessionAsync(string connectionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionId);

        try
        {
            if (_activeSessions.TryRemove(connectionId, out var session))
            {
                // Cancel the session and trigger completion of the streaming task
                session.CancellationTokenSource.Cancel();
                session.IsSessionActive = false;

                // If we have transcribed text, create a final message
                var finalText = session.TranscribedText.ToString().Trim();
                if (!string.IsNullOrEmpty(finalText))
                {
                    // Send final transcription
                    await signalRService.SendAsync(session.ConnectionId, "ReceiveTranscription", new
                    {
                        Text = finalText,
                        IsComplete = true
                    });

                    // If we have user info, also publish the final transcription to agents
                    if (session.UserId != null && session.ConversationId != null)
                    {
                        await PublishTranscriptionToAgents(session.UserId, session.ConversationId, finalText);
                    }
                }

                // Publish audio session ended event to transition back to text-based conversation
                if (session.UserId != null && session.ConversationId != null)
                {
                    await PublishVoiceSessionEndedEvent(session.UserId, session.ConversationId);
                }

                logger.LogInformation("Ended realtime audio session for connection {ConnectionId}", connectionId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error ending streaming session for {ConnectionId}", connectionId);
        }
    }

    private async Task PublishTranscriptionToAgents(string userId, string conversationId, string text)
    {
        try
        {
            // Use Orleans to send the transcription to agents
            var streamProvider = clusterClient.GetStreamProvider(OrleansStreamProvider);
            var streamId = StreamId.Create(OrleansNamespace, $"{userId}/{conversationId}");
            var stream = streamProvider.GetStream<Event>(streamId);

            var data = new Dictionary<string, string>
            {
                { "userId", userId },
                { "userMessage", text },
                { "isVoiceInput", "true" }
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventType.UserChatInput),
                Data = data
            });

            logger.LogInformation("Published voice transcription for user {UserId}: {Text}", userId, text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing transcription to agents");
        }
    }

    private async Task PublishVoiceSessionEndedEvent(string userId, string conversationId)
    {
        try
        {
            var streamProvider = clusterClient.GetStreamProvider(OrleansStreamProvider);
            var streamId = StreamId.Create(OrleansNamespace, $"{userId}/{conversationId}");
            var stream = streamProvider.GetStream<Event>(streamId);

            var data = new Dictionary<string, string>
            {
                { "userId", userId },
                { "message", "Voice session ended. Continuing with text chat." }
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventType.VoiceSessionEnded),
                Data = data
            });

            logger.LogInformation("Published voice session ended event for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error publishing voice session ended event");
        }
    }

    private async Task RouteToAgentBasedOnIntent(string userId, string conversationId, string message, string agentType, string userIntent, string eventType)
    {
        try
        {
            logger.LogInformation("Routing voice request to {AgentType} agent with intent: {UserIntent}, event: {EventType}",
                agentType, userIntent, eventType);

            var streamProvider = clusterClient.GetStreamProvider(OrleansStreamProvider);
            var streamId = StreamId.Create(OrleansNamespace, $"{userId}/{conversationId}");
            var stream = streamProvider.GetStream<Event>(streamId);

            // Use the eventType directly from the function call
            var data = new Dictionary<string, string>
            {
                { "userId", userId },
                { "userMessage", message },
                { "userIntent", userIntent },
                { "isVoiceInput", "true" }
            };

            await stream.OnNextAsync(new Event
            {
                Type = eventType,
                Data = data
            });

            logger.LogInformation("Published voice request to {EventType} for user {UserId}", eventType, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error routing voice request to agent");
        }
    }

    public record RealtimeSession(string ConnectionId)
    {
        public ConcurrentQueue<byte[]> AudioChunks { get; } = new();
        public StringBuilder TranscribedText { get; } = new();
        public CancellationTokenSource CancellationTokenSource { get; } = new();
        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        public bool IsSessionActive { get; set; }
        public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;
        public string? UserId { get; set; }
        public string? ConversationId { get; set; }

        public void AddAudioChunk(byte[] chunk)
        {
            AudioChunks.Enqueue(chunk);
            LastActivity = DateTimeOffset.UtcNow;
        }
    }
}
