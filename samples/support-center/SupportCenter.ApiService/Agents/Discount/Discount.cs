using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Events;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.Agents.Discount;

[ImplicitStreamSubscription(OrleansNamespace)]
public class Discount : AiAgent<DiscountState>
{
    private readonly ILogger<Discount> _logger;

    protected override string Namespace => OrleansNamespace;

    public Discount([PersistentState("state", "messages")] IPersistentState<AgentState<DiscountState>> state,
        ILogger<Discount> logger,
        IChatClient chatClient)
    : base(state)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }
                break;
            default:
                break;
        }
    }
}