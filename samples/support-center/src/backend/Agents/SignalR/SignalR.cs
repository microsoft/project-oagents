using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SignalRHub;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class SignalR : Agent
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<SignalR> _logger;
    private readonly ISignalRService _signalRClient;

    public SignalR(ILogger<SignalR> logger, ISignalRService signalRClient)
    {
        _logger = logger;
        _signalRClient = signalRClient;
    }

    public async override Task HandleEvent(Event item)
    {
        string? messageId = item.Data.GetValueOrDefault<string>("id");
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? answer = item.Data.GetValueOrDefault<string>("answer");

        var type = AgentType.Unknown;

        switch (item.Type)
        {
            case nameof(EventType.QnARetrieved):
                type = AgentType.QnA;
                break;
            case nameof(EventType.InvoiceRetrieved):
                type = AgentType.Invoice;
                break;
            case nameof(EventType.CustomerInfoRetrieved):
                type = AgentType.CustomerInfo;
                break;
            case nameof(EventType.DiscountRetrieved):
                type = AgentType.Discount;
                break;
            default:
                break;
        }

        if (type != AgentType.Unknown && userId != null && answer != null)
            await _signalRClient.SendMessageToSpecificClient(messageId, userId, answer, type);
    }
}