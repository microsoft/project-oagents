using Marketing.Events;
using Marketing.Options;
using Marketing.SignalRHub;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class SignalR : Agent
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<SignalR> _logger;
    private readonly ISignalRClient _signalRClient;

    public SignalR(ILogger<SignalR> logger, ISignalRClient signalRClient)
    {
        _logger = logger;
        _signalRClient = signalRClient;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.ArticleWritten):
                var writenArticle = item.Message;
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], writenArticle, AgentTypes.Chat);
                break;
            default:
                break;
        }
    }
}