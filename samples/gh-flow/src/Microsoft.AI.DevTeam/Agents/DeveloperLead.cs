using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace Microsoft.AI.DevTeam.Agents;
[ImplicitStreamSubscription(Consts.MainNamespace)]
public class DeveloperLead([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperLeadState>> state, IChatClient chatClient, ILogger<DeveloperLead> logger) : AiAgent<DeveloperLeadState>(state), ILeadDevelopers
{
    protected override string Namespace => Consts.MainNamespace;

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.DevPlanRequested):
                {
                    var context = item.ToGithubContext();
                    var plan = await CreatePlan(item.Data["input"]);
                    var data = context.ToData();
                    data["result"] = JsonSerializer.Serialize(plan);
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
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
                    var latestPlan = _state.State.History.Last().Message;
                    var data = context.ToData();
                    data["plan"] = latestPlan;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
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
    public async Task<DevLeadPlanResponse> CreatePlan(string ask)
    {
        try
        {
            // TODO: Ask the architect for the existing high level architecture
            // as well as the file structure
            var input = AppendChatHistory(ask);
            var instruction = "Consider the following architectural guidelines:!waf!";
            var guidelines = ""; // await AddKnowledge(instruction, "waf");
            var prompt = $$"""
                            You are a Dev Lead for an application team, building the application described below. 
                            Please break down the steps and modules required to develop the complete application, describe each step in detail.
                            Make prescriptive architecture, language, and frameowrk choices, do not provide a range of choices. 
                            For each step or module then break down the steps or subtasks required to complete that step or module.
                            For each subtask write an LLM prompt that would be used to tell a model to write the coee that will accomplish that subtask.  If the subtask involves taking action/running commands tell the model to write the script that will run those commands. 
                            In each LLM prompt restrict the model from outputting other text that is not in the form of code or code comments. 
                            Please output a JSON array data structure, in the precise schema shown below, with a list of steps and a description of each step, and the steps or subtasks that each requires, and the LLM prompts for each subtask. 
                            Example: 
                                {
                                    "steps": [
                                        {
                                            "step": "1",
                                            "description": "This is the first step",
                                            "subtasks": [
                                                {
                                                    "subtask": "Subtask 1",
                                                    "description": "This is the first subtask",
                                                    "prompt": "Write the code to do the first subtask"
                                                },
                                                {
                                                    "subtask": "Subtask 2",
                                                    "description": "This is the second subtask",
                                                    "prompt": "Write the code to do the second subtask"
                                                }
                                            ]
                                        }
                                    ]
                                }
                            Do not output any other text. 
                            Do not wrap the JSON in any other text, output the JSON format described above, making sure it's a valid JSON.
                            Input: {{input}}
                            {{guidelines}}
                            """;
            var result = await chatClient.GetResponseAsync<DevLeadPlanResponse>(prompt);
            return result.Result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating development plan");
            return null;
        }
    }
}

public interface ILeadDevelopers
{
    public Task<DevLeadPlanResponse> CreatePlan(string ask);
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