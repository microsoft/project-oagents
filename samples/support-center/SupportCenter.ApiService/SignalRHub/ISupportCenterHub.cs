namespace SupportCenter.ApiService.SignalRHub;

public interface ISupportCenterHub
{
    public Task ConnectToAgent(string userId);

    public Task ChatMessage(ChatMessage frontEndMessage, IClusterClient clusterClient);

    public Task SendMessageToSpecificClient(string userId, string message);
}
