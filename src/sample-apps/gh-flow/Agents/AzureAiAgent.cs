using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Orleans.Runtime;
using Microsoft.AI.Agents.Abstractions;

namespace Microsoft.AI.DevTeam;

public abstract class AzureAiAgent<T> : AiAgent<T>
{
    private readonly IKernelMemory _memory;

    public AzureAiAgent([PersistentState("state", "messages")] IPersistentState<AgentState<T>> state, IKernelMemory memory) : base(state)
    {
        _memory = memory;
    }

    protected async Task<KernelArguments> AddWafContext(IKernelMemory memory, KernelArguments arguments)
    {
        var waf = await memory.AskAsync(arguments["input"].ToString(), index:"waf");
        arguments["wafContext"] = $"Consider the following architectural guidelines: ${waf}";
        return arguments;
    }

    protected override async Task<string> CallFunction(string template, KernelArguments arguments, Kernel kernel)
    {
       var wafArguments = await AddWafContext(_memory, arguments);
       return await base.CallFunction(template, wafArguments, kernel);
    }
}