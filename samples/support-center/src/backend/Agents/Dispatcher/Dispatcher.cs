using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Options;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Dispatcher : AiAgent<DispatcherState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<Dispatcher> _logger;

    public Dispatcher(
        [PersistentState("state", "messages")] IPersistentState<AgentState<DispatcherState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<Dispatcher> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            
            default:
                break;
        }
    }

    private async Task SendDispatcherEvent(string userId, string customerInfo)
    {
    }
}