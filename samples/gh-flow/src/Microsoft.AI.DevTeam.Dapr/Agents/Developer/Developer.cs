using CloudNative.CloudEvents;
using Dapr.Actors.Runtime;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Microsoft.AI.DevTeam;

public class Dev : AiAgent<DeveloperState>, IDevelopApps
{
    protected override string Namespace => Consts.MainNamespace;
    
    private readonly ILogger<Dev> _logger;

    public Dev(ActorHost host, Kernel kernel, ISemanticTextMemory memory, ILogger<Dev> logger) 
    : base(host, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.CodeGenerationRequested):
                // var code = await GenerateCode(item.Message);
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                // {
                //     Type = nameof(GithubFlowEventType.CodeGenerated),
                //     Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             { "code", code }
                //         },
                //     Message = code
                // });
                break;
            case nameof(GithubFlowEventType.CodeChainClosed):
                // var lastCode = _state.State.History.Last().Message;
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                // {
                //     Type = nameof(GithubFlowEventType.CodeCreated),
                //     Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             { "code", lastCode },
                //             { "parentNumber", item.Data["parentNumber"] }
                //         },
                //     Message = lastCode
                // });
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
            return await CallFunction(DeveloperSkills.Implement, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            return default;
        }
    }
}

public class DeveloperState
{
    public string Understanding { get; set; }
}

public interface IDevelopApps
{
    public Task<string> GenerateCode(string ask);
}

public class UnderstandingResult
{
    public string NewUnderstanding { get; set; }
    public string Explanation { get; set; }
}