using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Writer : AiAgent<WriterState>, IWriter
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<Writer> _logger;

    public Writer([PersistentState("state", "messages")] IPersistentState<AgentState<WriterState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Writer> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
        if(state.State.Data == null)
        {
            state.State.Data = new WriterState();
        }
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserChatInput):                
                //var lastCode = _state.State.History.Last().Message;

                _logger.LogInformation($"[{nameof(Writer)}] Event {nameof(EventTypes.UserChatInput)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
                string newArticle = await CallFunction(WriterPrompts.Write, context);
                _state.State.Data.WrittenArticle = newArticle;

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
        return Task.FromResult(_state.State.Data.WrittenArticle);
    }
}

public interface IWriter : IGrainWithStringKey
{
    Task<String> GetArticle();
}

[GenerateSerializer]
public class WriterState
{
    [Id(0)]
    public string WrittenArticle { get; set; }
}
[GenerateSerializer]
public class UnderstandingResult
{
    [Id(0)]
    public string NewUnderstanding { get; set; }
    [Id(1)]
    public string Explanation { get; set; }
}