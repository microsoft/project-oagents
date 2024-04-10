using CloudNative.CloudEvents;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Dev : AiAgent<DeveloperState>, IDevelopApps
{
    protected override string Namespace => Consts.MainNamespace;

    private readonly ILogger<Dev> _logger;

    public Dev([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Dev> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.CodeGenerationRequested):
                {
                    var data = (JObject)item.Data;
                    var code = await GenerateCode(data["input"].ToString());
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.CodeGenerated),
                        Data = new Dictionary<string, string> {
                            { "org", data["org"].ToString() },
                            { "repo", data["repo"].ToString() },
                            { "issueNumber", data["issueNumber"].ToString()},
                            { "result", code }
                        }
                    });
                }

                break;
            case nameof(GithubFlowEventType.CodeChainClosed):
                {
                    var data = (JObject)item.Data;
                    var lastCode = _state.State.History.Last().Message;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.CodeCreated),
                        Data = new Dictionary<string, string> {
                            { "org", data["org"].ToString() },
                            { "repo", data["repo"].ToString() },
                            { "issueNumber", data["issueNumber"].ToString() },
                            { "code", lastCode },
                            { "parentNumber", data["parentNumber"].ToString() }
                        }
                    });
                }

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
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask) };
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf", context);
            return await CallFunction(DeveloperSkills.Implement, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating code");
            return default;
        }
    }
}

[GenerateSerializer]
public class DeveloperState
{
    [Id(0)]
    public string Understanding { get; set; }
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