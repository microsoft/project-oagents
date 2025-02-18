using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.SignalRHub;
using System.Collections.Concurrent;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.Agents.SignalR;

[ImplicitStreamSubscription(OrleansNamespace)]
public class SignalR : Agent
{
    protected override string Namespace => OrleansNamespace;
    protected override string StreamProvider => OrleansStreamProvider;
    private readonly ConcurrentDictionary<string, AgentType> _eventTypeToSenderTypeMapping = new()
    {
        [nameof(EventType.QnARetrieved)] = AgentType.QnA,
        [nameof(EventType.QnANotification)] = AgentType.QnA,
        [nameof(EventType.InvoiceRetrieved)] = AgentType.Invoice,
        [nameof(EventType.InvoiceNotification)] = AgentType.Invoice,
        [nameof(EventType.DispatcherNotification)] = AgentType.Dispatcher,
        [nameof(EventType.CustomerInfoRetrieved)] = AgentType.CustomerInfo,
        [nameof(EventType.CustomerInfoNotification)] = AgentType.CustomerInfo,
        [nameof(EventType.DiscountRetrieved)] = AgentType.Discount,
        [nameof(EventType.DiscountNotification)] = AgentType.Discount,
        [nameof(EventType.AgentNotification)] = AgentType.Notification,
        [nameof(EventType.ConversationRetrieved)] = AgentType.Conversation,
        [nameof(EventType.Unknown)] = AgentType.Unknown
    };

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

        if (userId == null)
        {
            return;
        }

        if (!_eventTypeToSenderTypeMapping.TryGetValue(item.Type, out AgentType agentType))
            return;

        if (agentType == AgentType.Unknown)
        {
            _logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. This event is not supported.", nameof(SignalR), item.Type, item.Data);
            message = "Sorry, I don't know how to handle this request. Try to rephrase it.";
        }

        var registry = GrainFactory.GetGrain<IStoreConnections>(userId);
        var connection =  await registry.GetConnection();
        await _signalRClient.SendMessageToClient(messageId: Guid.NewGuid().ToString(), userId, conversationId: connection.ConversationId, connectionId: connection.Id, message, agentType);
    }
}