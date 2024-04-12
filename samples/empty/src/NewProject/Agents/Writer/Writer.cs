using BoilerPlate.Events;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Writer : AiAgent<WriterState>
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
            case nameof(EventTypes.NewRequest):                
                if (Int32.TryParse(item.Message, out int counter))
                {
                    //var lastCode = _state.State.History.Last().Message;

                    this._state.State.Data.counter += counter;
                    _logger.LogInformation($"[{nameof(Writer)}] Event {nameof(EventTypes.NewRequest)}. Counter: {this._state.State.Data.counter}");
                    
                    var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
                    await CallFunction(WriterPrompts.Write, context);
                    //await AddKnowledge(instruction, "waf", context);
                }
                else
                {
                    _logger.LogError($"[{nameof(Writer)}] Failed to parse message {item.Message} to int");
                }
                
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
}

[GenerateSerializer]
public class WriterState
{
    [Id(0)]
    public string WrittenArticle { get; set; }

    [Id(1)]
    public int counter { get; set;  }
}
[GenerateSerializer]
public class UnderstandingResult
{
    [Id(0)]
    public string NewUnderstanding { get; set; }
    [Id(1)]
    public string Explanation { get; set; }
}