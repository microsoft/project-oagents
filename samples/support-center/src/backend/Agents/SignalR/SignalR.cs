using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using SupportCenter.Events;
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
        switch (item.Type)
        {
            case nameof(EventType.QnARetrieved):
                var qnaResponse = item.Data["qnaResponse"]; 
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], qnaResponse, AgentTypes.Chat);
                break;

            case nameof(EventType.InvoiceRetrieved):
                var invoice = item.Data["invoice"];
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], invoice, AgentTypes.Invoice);
                break;

            case nameof(EventType.CustomerInfoRetrieved):
                var customerInfo = item.Data["customerInfo"]; 
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], customerInfo, AgentTypes.CustomerInfo);
                break;

            case nameof(EventType.DiscountRetrieved):
                var discount = item.Data["discount"];
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], discount, AgentTypes.Discount);
                break;

            default:
                break;
        }
    }
}