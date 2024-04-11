using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Writer : AiAgent<WriterState>, IDevelopApps
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<Writer> _logger;

    public Writer([PersistentState("state", "messages")] IPersistentState<AgentState<WriterState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Writer> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.ArticleWritten):
                var code = await GenerateCode(item.Message);
                _logger.LogInformation($"Executed agent {nameof(EventTypes.ArticleWritten)}: {this._state.State.Data.WrittenArticle}");
                //await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
                //{
                //    Type = nameof(EventTypes.ArticleWritten),
                //    Data = new Dictionary<string, string> {
                //            { "org", item.Data["org"] },
                //            { "repo", item.Data["repo"] },
                //            { "issueNumber", item.Data["issueNumber"] },
                //            { "code", code }
                //        },
                //    Message = code
                //});
                break;
            case nameof(EventTypes.NewRequest):
                var lastCode = _state.State.History.Last().Message;
                _logger.LogInformation($"Executed agent {nameof(EventTypes.NewRequest)}: {this._state.State.Data.WrittenArticle}");
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

    public async Task<string> GenerateCode(string ask)
    {
        try
        {
            // TODO: ask the architect for the high level architecture as well as the files structure of the project
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask)};
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf",context);
            return await CallFunction(WriterPrompts.Implement, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            return default;
        }
    }
}

[GenerateSerializer]
public class WriterState
{
    [Id(0)]
    public string WrittenArticle { get; set; }
}

public interface IDevelopApps
{
    public Task<string> GenerateCode(string ask);
}

[GenerateSerializer]
public class UnderstandingResult
{
    [Id(0)]
    public string NewUnderstanding { get; set; }
    [Id(1)]
    public string Explanation { get; set; }
}