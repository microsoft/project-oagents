using Marketing.Hubs;
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
    private readonly ISignalRClient _signalRClient;

    public Editor([PersistentState("state", "messages")] IPersistentState<AgentState<EditorState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Editor> logger, ISignalRClient signalRClient) 
    : base(state, memory, kernel)
    {
        _logger = logger;
        _signalRClient = signalRClient;

        if (state.State.Data == null)
        {
            state.State.Data = new EditorState();
        }
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserConnected):
                // The user reconnected, let's send the last message if we have one
                if (_state.State.History?.Last().Message == null)
                {
                    return;
                }
                var lastMessage = _state.State.History.Last().Message;
                await _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], lastMessage, AgentTypes.Chat);
                break;
            case nameof(EventTypes.UserChatInput):
                //item.Data.TryGetValue("context", out var context);
                //this._state.State.Data.Article = "I have written an article";

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