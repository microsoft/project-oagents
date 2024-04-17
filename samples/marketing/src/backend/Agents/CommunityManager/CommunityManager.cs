#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
using Marketing.Hubs;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CommunityManager : AiAgent<CommunityManagerState>, ICommunityManager
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ISignalRClient _signalRClient;
    private readonly ILogger<GraphicDesigner> _logger;

    public CommunityManager([PersistentState("state", "messages")] IPersistentState<AgentState<CommunityManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<GraphicDesigner> logger, ISignalRClient signalRClient) 
    : base(state, memory, kernel)
    {
        _signalRClient = signalRClient;
        _logger = logger;
        if(state.State.Data == null)
        {
            state.State.Data = new CommunityManagerState();
        }
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.ArticleWritten):                
                //var lastCode = _state.State.History.Last().Message;

                _logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleWritten)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
                string newPost = await CallFunction(CommunityManagerPrompts.WritePost, context);
                _state.State.Data.WrittenPost = newPost;

                _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], newPost, AgentTypes.CommunityManager);

                break;
            default:
                break;
        }
    }

    public Task<String> GetArticle()
    {
        return Task.FromResult(_state.State.Data.WrittenPost);
    }
}

public interface ICommunityManager : IGrainWithStringKey
{
    Task<String> GetArticle();
}

[GenerateSerializer]
public class CommunityManagerState
{
    [Id(0)]
    public string WrittenPost { get; set; }
}
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task