using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam.Abstractions;

public abstract class AzureAiAgent<T> : AiAgent<T>
{
    public AzureAiAgent([PersistentState("state", "messages")] IPersistentState<AgentState<T>> state) : base(state)
    {
        _state = state;
    }

    protected async Task<ContextVariables> AddWafContext(ISemanticTextMemory memory, ContextVariables context,  string ask)
    {
        var interestingMemories = memory.SearchAsync("waf-pages", ask, 2);
        var wafContext = "Consider the following architectural guidelines:";
        await foreach (var m in interestingMemories)
        {
            wafContext += $"\n {m.Metadata.Text}";
        }
        context.Set("wafContext", wafContext);
        return context;
    }

    protected override async Task<string> CallFunction(string template, string ask, ContextVariables context, IKernel kernel, ISemanticTextMemory memory)
    {
       var wafContext = await AddWafContext(memory, context, ask);
       return await base.CallFunction(template, ask, wafContext, kernel, memory);
    }
}
