using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Agents;
using SupportCenter.Options;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class QnA : AiAgent<QnAState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<QnA> _logger;

    public QnA([PersistentState("state", "messages")] IPersistentState<AgentState<QnAState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<QnA> logger) 
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
}