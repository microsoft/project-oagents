using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Dapr.Events;
using Microsoft.Extensions.AI;

namespace Microsoft.AI.DevTeam.Dapr;
public class DeveloperLead : AiAgent<DeveloperLeadState>, IDaprAgent
{
    private readonly ILogger<DeveloperLead> _logger;

    public DeveloperLead(ActorHost host, DaprClient client,IChatClient chatClient, ILogger<DeveloperLead> logger)
     : base(host, client, chatClient)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.DevPlanRequested):
                {
                     var context = item.ToGithubContext();
                    var plan = await CreatePlan(item.Data["input"]);
                    var data = context.ToData();
                    data["result"] = plan;
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new Event
                    {
                        Type = nameof(GithubFlowEventType.DevPlanGenerated),
                        Subject = context.Subject,
                        Data = data
                    });
                }
                break;
            case nameof(GithubFlowEventType.DevPlanChainClosed):
                 {
                    var context = item.ToGithubContext();
                    var latestPlan = state.History.Last().Message;
                    var data = context.ToData();
                    data["plan"] = latestPlan;
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new Event
                    {
                        Type = nameof(GithubFlowEventType.DevPlanCreated),
                        Subject = context.Subject,
                        Data = data
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

            return await CallFunction(DevLeadSkills.Plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating development plan");
            return default;
        }
    }
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