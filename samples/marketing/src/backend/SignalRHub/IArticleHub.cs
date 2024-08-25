namespace Marketing.SignalRHub;

public interface IArticleHub
{
    public Task ConnectToAgent(string SessionId);

    public Task ChatMessage(FrontEndMessage frontEndMessage, IClusterClient clusterClient);

    public Task SendMessageToSpecificClient(string SessionId, string message);
}
