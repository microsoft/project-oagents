using Azure;
using Azure.AI.OpenAI;
using Microsoft.AI.Agents.Abstractions;
using OpenAI;
using OpenAI.Audio;
using OpenAI.RealtimeConversation;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.SignalRHub;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.RealTimeAudio;

public class RealTimeAudioService : IRealTimeAudioService
{
    private readonly string _realtimeDeploymentName = Gpt4oRealtime; // Use the appropriate deployment name
    private readonly ConcurrentDictionary<string, RealtimeSession> _activeSessions = new();
    private readonly GeneratedSpeechVoice _speechVoice = GeneratedSpeechVoice.Alloy;
    // Maximum time a session can be inactive before being closed (5 minutes)
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(5);
    private readonly ILogger<RealTimeAudioService> logger;
    private readonly ISignalRService signalRService;
    private readonly AzureOpenAIClient azureOpenAIClient;
    private readonly AudioClient audioClient;
    private readonly IClusterClient clusterClient;

    private static readonly string[] agentTypes = ["Conversation", "QnA", "CustomerInfo", "Invoice", "Dispatcher"];
    private static readonly string[] eventTypes = ["UserChatInput", "ConversationRequested", "QnARequested", "CustomerInfoRequested", "InvoiceRequested"];
    private static readonly string[] requiredParams = ["agentType", "userIntent", "eventType"];

    public RealTimeAudioService(
        ILogger<RealTimeAudioService> logger,
        ISignalRService signalRService,
        [FromKeyedServices(Gpt4oRealtime)] AzureOpenAIClient azureOpenAIClient,
        [FromKeyedServices(Whisper)] AudioClient audioClient,
        IClusterClient clusterClient)
    {
        this.logger = logger;
        this.signalRService = signalRService;
        this.azureOpenAIClient = azureOpenAIClient;
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
            var realtimeClient = azureOpenAIClient.GetRealtimeConversationClient(_realtimeDeploymentName);

            // Fix for CS7036: Provide the required 'name' parameter when creating a ConversationFunctionTool instance.
            var routingFunction = new ConversationFunctionTool("route_user_query")
            {
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
                    required = requiredParams
                })
            };

            // Start a Realtime Conversation Session
            using var conversationSession = await realtimeClient.StartConversationSessionAsync(
                cancellationToken: session.CancellationToken);

            session.IsSessionActive = true;
            logger.LogInformation("Started GPT-4o Realtime conversation session");

            // Configure the session with our options
#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var sessionOptions = new ConversationSessionOptions()
            {
                // Use the same system prompt as before
                Instructions = @"You are part of a support center multi-agent system. The user is speaking with you through a voice interface. 
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

                // Configure input audio transcription
                InputTranscriptionOptions = new ConversationInputTranscriptionOptions
                {
                    Model = ConversationTranscriptionModel.Whisper1, // Use Whisper model for transcription
                },

                // Configure tools (functions)
                Tools =
                {
                    routingFunction
                },

            };
#pragma warning restore OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Apply the configuration
            await conversationSession.ConfigureSessionAsync(sessionOptions, session.CancellationToken);

            // Process updates from the conversation in a separate task
            var processingTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (ConversationUpdate update in conversationSession.ReceiveUpdatesAsync(session.CancellationToken))
                    {
                        await ProcessConversationUpdateAsync(session, update, isUserSpeaking: true, routingFunction);
                    }
                    ;
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Update processing cancelled for session {ConnectionId}", session.connectionId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing conversation updates for session {ConnectionId}: {Message}",
                        session.connectionId, ex.Message);
                }
            }, session.CancellationToken);

            // Handle audio chunks processing
            while (!session.CancellationToken.IsCancellationRequested && session.IsSessionActive)
            {
                try
                {
                    // Try to dequeue an audio chunk
                    if (session.AudioChunks.TryDequeue(out var audioChunk))
                    {
                        // Send audio chunk to the API
                        await conversationSession.SendInputAudioAsync(
                            BinaryData.FromBytes(audioChunk),
                            session.CancellationToken);
                    }

                    // Small delay to prevent CPU spiking
                    await Task.Delay(10, session.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Audio processing cancelled for session {ConnectionId}", session.connectionId);
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error sending audio chunk for session {ConnectionId}: {Message}",
                        session.connectionId, ex.Message);
                }
            }
            // Wait for the processing task to complete.
            await processingTask;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Realtime session {ConnectionId} was cancelled", session.connectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in realtime audio session for connection {ConnectionId}: {Message}",
                session.connectionId, ex.Message);
        }
        finally
        {
            session.IsSessionActive = false;
        }
    }

    private async Task ProcessConversationUpdateAsync(RealtimeSession session, ConversationUpdate update, bool isUserSpeaking, ConversationFunctionTool tool)
    {
        try
        {
            switch (update)
            {
                // [session.created] is the very first command on a session and lets us know that connection was successful.
                case ConversationSessionStartedUpdate sessionStartedUpdate:
                    logger.LogInformation("Session started with ID: {SessionId}",
                        sessionStartedUpdate.SessionId);
                    break;

                case ConversationSessionConfiguredUpdate sessionConfiguredUpdate:
                    // Session configuration applied
                    logger.LogInformation("Session configured successfully");
                    break;

                // [input_audio_buffer.speech_started] tells us that the beginning of speech was detected in the input audio
                // we're sending from the microphone.
                case ConversationInputSpeechStartedUpdate speechStartedUpdate:
                    logger.LogInformation("<<< Start of speech detected at {speechStartedUpdate.StartTimestamp}",
                        speechStartedUpdate.AudioStartTime);
                    break;

                // [input_audio_buffer.speech_stopped] tells us that the end of speech was detected in the input audio sent
                // from the microphone. It'll automatically tell the model to start generating a response to reply back.
                case ConversationInputSpeechFinishedUpdate speechFinishedUpdate:
                    logger.LogInformation(">>> End of speech detected at {speechFinishedUpdate.EndTimestamp}",
                        speechFinishedUpdate.AudioEndTime);
                    break;

                // [conversation.item.input_audio_transcription.completed] will only arrive if input transcription was
                // configured for the session. It provides a written representation of what the user said, which can
                // provide good feedback about what the model will use to respond.
                case ConversationInputTranscriptionFinishedUpdate transcriptionFinishedUpdate:
                    logger.LogInformation("Transcription completed: {Transcription}",
                        transcriptionFinishedUpdate.Transcript);
                    break;

                // Item streaming delta updates provide a combined view into incremental item data including output
                // the audio response transcript, function arguments, and audio data.
                case ConversationItemStreamingPartDeltaUpdate deltaUpdate:
                    // Handle audio responses
                    //Console.Write(deltaUpdate.AudioTranscript);
                    //Console.Write(deltaUpdate.Text);
                    //speakerOutput.EnqueueForPlayback(deltaUpdate.AudioBytes);
                    var audioData = deltaUpdate.AudioBytes;
                    if (audioData != null && audioData.Length > 0)
                    {
                        await signalRService.SendAsync(session.connectionId, "ReceiveAudioResponse", [audioData]);
                    }
                    break;

                // [response.output_item.done] tells us that a model-generated item with streaming content is completed.
                // That's a good signal to provide a visual break and perform final evaluation of tool calls.
                case ConversationItemStreamingFinishedUpdate itemFinishedUpdate:
                    // The complete response has been processed
                    if (
                        itemFinishedUpdate.FunctionName == tool.Name
                        && itemFinishedUpdate.FunctionCallArguments != null)
                    {
                        logger.LogInformation("Function call completed: {FunctionName}",
                            itemFinishedUpdate.FunctionName);

                        // Parse the function arguments
                        var arguments = JsonSerializer.Deserialize<Dictionary<string, string>>(
                            itemFinishedUpdate.FunctionCallArguments);

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
                    break;

                case ConversationErrorUpdate errorUpdate:
                    logger.LogInformation("Error occurred: {ErrorMessage}",
                        errorUpdate.Message);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing conversation update");
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
                    await signalRService.SendAsync(session.connectionId, "ReceiveTranscription",
                    [
                        new
                        {
                            Text = finalText,
                            IsComplete = true
                        }
                    ]);

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

    public record RealtimeSession(string connectionId)
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
