using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.Extensions.AI;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class ProductManager([PersistentState("state", "messages")] IPersistentState<AgentState<ProductManagerState>> state, IChatClient chatClient, ILogger<ProductManager> logger) : AiAgent<ProductManagerState>(state), IManageProducts
{
    protected override string Namespace => Consts.MainNamespace;

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.ReadmeRequested):
            {
                var context = item.ToGithubContext();
                var readme = await CreateReadme(item.Data["input"]);
                var data = context.ToData();
                data["result"]=readme;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                     Type = nameof(GithubFlowEventType.ReadmeGenerated),
                     Subject = context.Subject,
                     Data = data
                });
            }
                
                break;
            case nameof(GithubFlowEventType.ReadmeChainClosed):
            {
                var context = item.ToGithubContext();
                var lastReadme = _state.State.History.Last().Message;
                var data = context.ToData();
                data["readme"] = lastReadme;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                     Type = nameof(GithubFlowEventType.ReadmeCreated),
                     Subject = context.Subject,
                    Data = data
                });
            }
                
                break;
            default:
                break;
        }
    }

    public async Task<string> CreateReadme(string ask)
    {
        try
        {
            var input = AppendChatHistory(ask);
            var instruction = "Consider the following architectural guidelines:!waf!";
            var guidelines = "";// await AddKnowledge(instruction, "waf");
            var prompt = $$$""""
                            """
                            You are a program manager on a software development team. You are working on an app described below. 
                             Based on the input below, and any dialog or other context, please output a raw README.MD markdown file documenting the main features of the app and the architecture or code organization. 
                             Do not describe how to create the application. 
                             Write the README as if it were documenting the features and architecture of the application. You may include instructions for how to run the application. 
                            Input: {{input}}
                            {{guidelines}}
                            """";
            var result = await chatClient.GetResponseAsync(prompt);
            return result.Message.Text!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating readme");
            return default;
        }
    }
}

public interface IManageProducts
{
    public Task<string> CreateReadme(string ask);
}

[GenerateSerializer]
public class ProductManagerState
{
    [Id(0)]
    public string Capabilities { get; set; }
}