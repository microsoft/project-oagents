using BoilerPlate.Events;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Editor : AiAgent<EditorState>
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<Editor> _logger;

    public Editor([PersistentState("state", "messages")] IPersistentState<AgentState<EditorState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Editor> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
        if (state.State.Data == null)
        {
            state.State.Data = new EditorState();
        }
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.NewRequest):
                item.Data.TryGetValue("context", out var context);
                this._state.State.Data.Article = "I have written an article";
                break;
            default:
                break;
        }
    }

}

[GenerateSerializer]
public class EditorState
{
    [Id(0)]
    public string Article { get; set; }
}