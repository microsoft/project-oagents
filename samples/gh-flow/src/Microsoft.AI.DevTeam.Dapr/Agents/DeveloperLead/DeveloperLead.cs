using CloudNative.CloudEvents;
using Dapr.Actors.Runtime;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Microsoft.AI.DevTeam;
public class DeveloperLead : AiAgent<DeveloperLeadState>, ILeadDevelopers
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly ILogger<DeveloperLead> _logger;

    public DeveloperLead(ActorHost host, Kernel kernel, ISemanticTextMemory memory, ILogger<DeveloperLead> logger)
     : base(host, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.DevPlanRequested):
                // var plan = await CreatePlan(item.Message);
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                // {
                //     Type = nameof(GithubFlowEventType.DevPlanGenerated),
                //     Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             { "plan", plan }
                //         },
                //     Message = plan
                // });
                break;
            case nameof(GithubFlowEventType.DevPlanChainClosed):
                // var latestPlan = _state.State.History.Last().Message;
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                // {
                //     Type = nameof(GithubFlowEventType.DevPlanCreated),
                //     Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             {"parentNumber", item.Data["parentNumber"]},
                //             { "plan", latestPlan }
                //         },
                //     Message = latestPlan
                // });
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

public interface ILeadDevelopers
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