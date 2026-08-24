using Microsoft.AI.Agents.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans.Runtime;
using Orleans.Streams;

namespace Microsoft.AI.Agents.Orleans;

public abstract class MemoryBank : Grain, IGrainWithStringKey, IMemoryBank
{
    protected virtual string Namespace { get; set; }
    protected readonly Kernel _kernel;
    public MemoryBank(
        [PersistentState("memoryBanks", "memoryBanks")] IPersistentState<MemoryBankState> memory,
        Kernel kernel)
    {
        Memory = memory;
        _kernel = kernel;
    }
    public string Name { get; set; }
    public IPersistentState<MemoryBankState> Memory { get; set; }
    public abstract Task<string> ConsolidateMemory();

    public abstract Task<MemoryItem> ExtractMemory(Event item);

    public async Task HandleEvent(Event item)
    {
        await ProcessEvent(item);
    }

    public async Task ProcessEvent(Event item)
    {
        var memory = await ExtractMemory(item);
        if (memory != default)
        {
            if (Memory.State.Events == null) Memory.State.Events = new List<Event>();
            if (Memory.State.Memories == null) Memory.State.Memories = new List<MemoryItem>();
            Memory.State.Events.Add(item);
            Memory.State.Memories.Add(memory);
            var summary = await ConsolidateMemory();
            Memory.State.Summary = summary;
            await Memory.WriteStateAsync();
        }
    }

    public async Task PublishEvent(string ns, string id, Event item)
    {
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(ns, id);
        var stream = streamProvider.GetStream<Event>(streamId);
        await stream.OnNextAsync(item);
    }
    private async Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        await HandleEvent(item);
    }

    public async override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var streamProvider = this.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Namespace, this.GetPrimaryKeyString());
        var stream = streamProvider.GetStream<Event>(streamId);
        await stream.SubscribeAsync(HandleEvent);
    }

    protected async Task<string> CallFunction(string template, KernelArguments arguments, OpenAIPromptExecutionSettings? settings = null)
    {
        var propmptSettings = settings ?? new OpenAIPromptExecutionSettings { MaxTokens = 4096, Temperature = 0.5, TopP = 0.8 };
        var function = _kernel.CreateFunctionFromPrompt(template, propmptSettings);
        var result = (await _kernel.InvokeAsync(function, arguments)).ToString();
        return result;
    }

    public virtual Task<string> Recall()
    {
        return Task.FromResult(Memory.State.Summary);
    }
    public Task<MemoryBankState> IntrospectMemory()
    {
        return Task.FromResult(Memory.State);
    }
}
