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
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? message = item.Data.GetValueOrDefault<string>("message");

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
            case nameof(EventType.AgentNotification):
                type = AgentType.Notification;
                break;
            case nameof(EventType.Unknown):
                type = AgentType.Unknown;
                break;
            default:
                return;
        }

        if (type == AgentType.Unknown)
        {
            _logger.LogWarning($"[{nameof(SignalR)}] Event {item.Type} is not supported.");
            message = "Sorry, I don't know how to handle this request. Try to rephrase it.";
        }

        if (userId != null && message != null)
            await _signalRClient.SendMessageToClient(messageId: Guid.NewGuid().ToString(), userId, message, type);
    }
}