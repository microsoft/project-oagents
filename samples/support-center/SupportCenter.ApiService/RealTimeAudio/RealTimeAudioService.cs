using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Audio;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using static SupportCenter.ApiService.Consts;
using Azure.AI.OpenAI;
using Azure;
using System.Threading;
using System.Text;
using System.Text.Json;
using Microsoft.AI.Agents.Abstractions;
using Orleans;
using Orleans.Runtime;
using SupportCenter.ApiService.Events;

namespace SupportCenter.ApiService.RealTimeAudio;

public interface IRealTimeAudioService
{
    Task<string> TranscribeAudioAsync(byte[] audioData);
    Task<byte[]> SynthesizeSpeechAsync(string text);
    Task StartStreamingSessionAsync(string connectionId);
    Task EndStreamingSessionAsync(string connectionId);
    Task ProcessRealtimeAudioAsync(string connectionId, string userId, string conversationId, byte[] audioData);
}

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
        try
        {
            using var audioStream = new MemoryStream(audioData);
            
            // Note: This method still uses the existing transcription for non-realtime scenarios
            var response = await audioClient.TranscribeAudioAsync(audioStream, "audio");
            return response.Value.Text;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error transcribing audio");
            throw;
        }
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        
        try
        {
            var options = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Wav,
                SpeedRatio = 1.0f,
            };

            var result = await audioClient.GenerateSpeechAsync(text, _speechVoice, options);
            
            // Copy the result to a byte array
            using var memoryStream = new MemoryStream();
            await result.Value.AudioData.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error synthesizing speech");
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
            // Set up GPT-4o Realtime API options
            var options = new AudioRealtimeOptions
            {
                // Configure with appropriate settings
                Temperature = 0.7f,
                MaxTokens = 800,
                AudioOutputFormat = "wav",
                // Add system prompt to inform GPT-4o about its role in the multi-agent system
                SystemPrompt = "You are part of a support center multi-agent system. The user is speaking with you through a voice interface. " +
                               "You will transcribe their speech and provide helpful responses. Your response will be converted to speech. " +
                               "Keep responses concise and focused on the user's needs. When the voice session ends, the conversation will " +
                               "continue as text with other specialized agents based on the user's needs."
            };
            
            // Create streaming session
            var audioRealtimeClient = openAIClient.GetAudioRealtimeClient(_realtimeDeploymentName);
            
            // Start the streaming session
            await using var sessionStream = await audioRealtimeClient.GetStreamingSessionAsync(options, session.CancellationToken);
            session.IsSessionActive = true;
            
            // Handle audio chunks processing
            while (!session.CancellationToken.IsCancellationRequested && session.IsSessionActive)
            {
                // Try to dequeue an audio chunk
                if (session.AudioChunks.TryDequeue(out var audioChunk))
                {
                    // Send audio chunk to the API
                    await sessionStream.SendAudioAsync(
                        BinaryData.FromBytes(audioChunk),
                        session.CancellationToken);
                }
                
                // Process responses from GPT-4o
                await foreach (var response in sessionStream.GetResponsesAsync(session.CancellationToken))
                {
                    switch (response.Type)
                    {
                        case AudioRealtimeResponseType.TextDelta:
                            // Handle transcribed text
                            var text = response.TextDelta?.Text;
                            if (!string.IsNullOrEmpty(text))
                            {
                                session.TranscribedText.Append(text);
                                
                                // Send partial transcription to client
                                await signalRService.SendAsync(session.ConnectionId, "ReceivePartialTranscription", text);

                                // If we detect that the session has user data, publish transcription for agents to process
                                if (session.UserId != null && session.ConversationId != null && text.EndsWith(".") || text.EndsWith("?") || text.EndsWith("!"))
                                {
                                    await PublishTranscriptionToAgents(session.UserId, session.ConversationId, session.TranscribedText.ToString());
                                    
                                    // Clear the buffer after publishing a sentence
                                    session.TranscribedText.Clear();
                                }
                            }
                            break;
                        
                        case AudioRealtimeResponseType.AudioData:
                            // Handle audio response
                            var audioData = response.AudioData?.Data;
                            if (audioData != null)
                            {
                                // Send audio response to client
                                await signalRService.SendAsync(session.ConnectionId, "ReceiveAudioResponse", audioData.ToArray());
                            }
                            break;
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

    private record RealtimeSession(string ConnectionId)
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
