using Microsoft.AI.DevTeam.Abstractions;
using Microsoft.AI.DevTeam.Skills;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Orleans.Runtime;
using Orleans.Streams;
using System.Text.Json;

namespace Microsoft.AI.DevTeam;
[ImplicitStreamSubscription(Consts.MainNamespace)]
public class DeveloperLead : AiAgent<DeveloperLeadState>, ILeadDevelopers
{
    private readonly IKernel _kernel;
    private readonly ISemanticTextMemory _memory;
    private readonly ILogger<DeveloperLead> _logger;

    public DeveloperLead([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperLeadState>> state, IKernel kernel, ISemanticTextMemory memory, ILogger<DeveloperLead> logger) : base(state)
    {
        _kernel = kernel;
        _memory = memory;
        _logger = logger;
    }

    public async override Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        switch (item.Type)
        {
            case EventType.DevPlanRequested:
                var plan = await CreatePlan(item.Message);
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                {
                    Type = EventType.DevPlanGenerated,
                    Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            { "plan", plan }
                        },
                    Message = plan
                });
                break;
            case EventType.DevPlanChainClosed:
                var latestPlan = _state.State.History.Last().Message;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                {
                    Type = EventType.DevPlanCreated,
                    Data = new Dictionary<string, string> {
                            { "org", item.Data["org"] },
                            { "repo", item.Data["repo"] },
                            { "issueNumber", item.Data["issueNumber"] },
                            {"parentNumber", item.Data["parentNumber"]},
                            { "plan", latestPlan }
                        },
                    Message = latestPlan
                });
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
            var context = new ContextVariables(ask);
            return await CallFunction(DevLead.Plan, ask,context, _kernel, _memory);
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

[GenerateSerializer]
public class DevLeadPlanResponse
{
    [Id(0)]
    public List<Step> steps { get; set; }
}

[GenerateSerializer]
public class Step
{
    [Id(0)]
    public string description { get; set; }
    [Id(1)]
    public string step { get; set; }
    [Id(2)]
    public List<Subtask> subtasks { get; set; }
}

[GenerateSerializer]
public class Subtask
{
    [Id(0)]
    public string subtask { get; set; }
    [Id(1)]
    public string prompt { get; set; }
}

public class DeveloperLeadState
{
    public string Plan { get; set; }
}