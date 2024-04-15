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
    
    private readonly ILogger<CommunityManager> _logger;

    public CommunityManager([PersistentState("state", "messages")] IPersistentState<AgentState<CommunityManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<CommunityManager> logger) 
    : base(state, memory, kernel)
    {
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

                _logger.LogInformation($"[{nameof(CommunityManager)}] Event {nameof(EventTypes.ArticleWritten)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
                string newPost = await CallFunction(CommunityManagerPrompts.WritePost, context);
                _state.State.Data.WrittenPost = newPost;

                ArticleHub._allHubs.TryGetValue(item.Data["UserId"], out var articleHub);
                articleHub.SendMessageToSpecificClient(item.Data["UserId"], newPost);

                //await AddKnowledge(instruction, "waf", context);

                //await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
                //{
                //    Type = nameof(EventTypes.ArticleWritten),
                //    Data = new Dictionary<string, string> {
                //            { "org", item.Data["org"] },
                //            { "repo", item.Data["repo"] },
                //            { "issueNumber", item.Data["issueNumber"] },
                //            { "code", lastCode },
                //            { "parentNumber", item.Data["parentNumber"] }
                //        },
                //    Message = lastCode
                //});
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