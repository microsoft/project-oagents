using CloudNative.CloudEvents;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Dapr.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json.Linq;
namespace Microsoft.AI.DevTeam.Dapr;
public class Dev : AiAgent<DeveloperState>, IDevelopApps
{
    
    private readonly ILogger<Dev> _logger;

    public Dev(ActorHost host, DaprClient client, Kernel kernel, ISemanticTextMemory memory, ILogger<Dev> logger) 
    : base(host, client, memory, kernel)
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
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.CodeGenerated),
                        Subject = item.Subject,
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
                    var lastCode = state.History.Last().Message;
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.CodeCreated),
                        Subject = item.Subject,
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

public interface IDevelopApps : IActor
{
    public Task<string> GenerateCode(string ask);
}

public class UnderstandingResult
{
    public string NewUnderstanding { get; set; }
    public string Explanation { get; set; }
}