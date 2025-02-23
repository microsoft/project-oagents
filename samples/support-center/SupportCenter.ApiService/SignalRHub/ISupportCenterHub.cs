namespace SupportCenter.ApiService.SignalRHub;

public interface ISupportCenterHub
{
    public Task ConnectToAgent(string userId);
    public Task ChatMessage(ChatMessage frontEndMessage, IClusterClient clusterClient);
    public Task SendMessageToSpecificClient(string userId, string message);

    // New voice interaction methods
    Task StartVoiceInteraction(string userId, string conversationId);
    Task ProcessVoiceInput(string userId, string conversationId, byte[] audioData);
    Task EndVoiceInteraction(string userId, string conversationId);
}
