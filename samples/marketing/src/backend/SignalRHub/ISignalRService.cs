namespace Marketing.SignalRHub;
public interface ISignalRService
{
    Task SendMessageToSpecificClient(string SessionId, string message, AgentTypes agentType);
}
