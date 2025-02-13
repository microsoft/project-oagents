using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.Extensions.AI;

namespace Microsoft.AI.DevTeam.Agents;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Dev([PersistentState("state", "messages")] IPersistentState<AgentState<DeveloperState>> state, IChatClient chatClient, ILogger<Dev> logger) : AiAgent<DeveloperState>(state), IDevelopApps
{
    protected override string Namespace => Consts.MainNamespace;

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.CodeGenerationRequested):
                {
                    var context = item.ToGithubContext();
                    var code = await GenerateCode(item.Data["input"]);
                    var data = context.ToData();
                    data["result"] = code;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.CodeGenerated),
                        Subject = context.Subject,
                        Data = data
                    });
                }

                break;
            case nameof(GithubFlowEventType.CodeChainClosed):
                {
                    var context = item.ToGithubContext();
                    var lastCode = _state.State.History.Last().Message;
                    var data = context.ToData();
                    data["code"] = lastCode;
                    await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event
                    {
                        Type = nameof(GithubFlowEventType.CodeCreated),
                        Subject = context.Subject,
                        Data = data
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
            var input = AppendChatHistory(ask);
            var instruction = "Consider the following architectural guidelines:!waf!";
            var guidelines = "";// await AddKnowledge(instruction, "waf"); // TODO: fetch from Vector Store
            var prompt = $"""
                            You are a Developer for an application. 
                            Please output the code required to accomplish the task assigned to you below and wrap it in a bash script that creates the files.
                            Do not use any IDE commands and do not build and run the code.
                            Make specific choices about implementation. Do not offer a range of options.
                            Use comments in the code to describe the intent. Do not include other text other than code and code comments.
                            Input: {input}
                            {guidelines}
                            """;
            var result = await chatClient.GetResponseAsync(prompt);
            return result.Message.Text!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating code");
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