using SupportCenter.SignalRHub;

namespace SupportCenter.SignalRHub;

public interface ISupportCenterHub
{
    public Task ConnectToAgent(string UserId);

    public Task ChatMessage(FrontEndMessage frontEndMessage, IClusterClient clusterClient);

    public Task SendMessageToSpecificClient(string userId, string message);
}
