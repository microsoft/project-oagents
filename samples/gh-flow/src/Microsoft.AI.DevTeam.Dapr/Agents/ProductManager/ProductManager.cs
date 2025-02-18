using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Dapr.Events;
using Microsoft.Extensions.AI;

namespace Microsoft.AI.DevTeam.Dapr;

public class ProductManager : AiAgent<ProductManagerState>, IDaprAgent
{
    private readonly ILogger<ProductManager> _logger;

    public ProductManager(ActorHost host, DaprClient client, IChatClient chatClient, ILogger<ProductManager> logger)
    : base(host, client, chatClient)
    {
        _logger = logger;
    }

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
                    await PublishEvent(Consts.PubSub,Consts.MainTopic, new Event {
                        Type = nameof(GithubFlowEventType.ReadmeGenerated),
                        Subject = context.Subject,
                        Data = data
                    });
                }
                break;
            case nameof(GithubFlowEventType.ReadmeChainClosed):
                {
                    var context = item.ToGithubContext();
                var lastReadme = state.History.Last().Message;
                var data = context.ToData();
                data["readme"] = lastReadme;
                await PublishEvent(Consts.PubSub,Consts.MainTopic, new Event {
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
            return await CallFunction(PMSkills.Readme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating readme");
            return default;
        }
    }
}

public class ProductManagerState
{
    public string Capabilities { get; set; }
}