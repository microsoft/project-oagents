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
public class DeveloperLead : AiAgent<DeveloperLeadState>, ILeadDevelopers
{
    private readonly ILogger<DeveloperLead> _logger;

    public DeveloperLead(ActorHost host, DaprClient client, Kernel kernel, ISemanticTextMemory memory, ILogger<DeveloperLead> logger)
     : base(host, client, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.DevPlanRequested):
                {
                    var data = (JObject)item.Data;
                    var plan = await CreatePlan(data["input"].ToString());
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.DevPlanGenerated),
                        Data = new Dictionary<string, string> {
                            { "org", data["org"].ToString() },
                            { "repo", data["repo"].ToString() },
                            { "issueNumber", data["issueNumber"].ToString() },
                            { "result", plan }
                        }
                    });
                }
                break;
            case nameof(GithubFlowEventType.DevPlanChainClosed):
                 {
                    var data = (JObject)item.Data;
                    var latestPlan = state.History.Last().Message;
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new CloudEvent
                    {
                        Type = nameof(GithubFlowEventType.DevPlanCreated),
                        Data = new Dictionary<string, string> {
                            { "org", data["org"].ToString() },
                            { "repo", data["repo"].ToString() },
                            { "issueNumber", data["issueNumber"].ToString() },
                            {"parentNumber", data["parentNumber"].ToString()},
                            { "plan", latestPlan }
                        }
                    });
                }
                break;
            default:
                break;
        }
    }
    public async Task<string> CreatePlan(string ask)
    {
        try
        {
            // TODO: Ask the architect for the existing high level architecture
            // as well as the file structure
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask) };
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf", context);
            return await CallFunction(DevLeadSkills.Plan, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating development plan");
            return default;
        }
    }
}

public interface ILeadDevelopers : IActor
{
    public Task<string> CreatePlan(string ask);
}

public class DevLeadPlanResponse
{
    public List<Step> steps { get; set; }
}

public class Step
{
    public string description { get; set; }
    public string step { get; set; }
    public List<Subtask> subtasks { get; set; }
}

public class Subtask
{
    public string subtask { get; set; }
    public string prompt { get; set; }
}

public class DeveloperLeadState
{
    public string Plan { get; set; }
}