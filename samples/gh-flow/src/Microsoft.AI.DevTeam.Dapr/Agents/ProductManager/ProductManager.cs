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

public class ProductManager : AiAgent<ProductManagerState>, IManageProducts
{
    private readonly ILogger<ProductManager> _logger;

    public ProductManager(ActorHost host, DaprClient client, Kernel kernel, ISemanticTextMemory memory, ILogger<ProductManager> logger)
    : base(host, client, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.ReadmeRequested):
                {
                    var data = (JObject)item.Data;
                    var readme = await CreateReadme(data["input"].ToString());
                    await PublishEvent(Consts.PubSub, Consts.MainTopic, new CloudEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = nameof(GithubFlowEventType.ReadmeGenerated),
                        Subject = item.Subject,
                        Data = new Dictionary<string, string> {
                            { "org", data["org"].ToString() },
                            { "repo", data["repo"].ToString() },
                            { "issueNumber", data["issueNumber"].ToString() },
                            { "result", readme }
                        }
                    });
                }
                break;
            case nameof(GithubFlowEventType.ReadmeChainClosed):
                {
                    var data = (JObject)item.Data;
                    var lastReadme = state.History.Last().Message;
                    await PublishEvent(Consts.PubSub, Consts.MainTopic, new CloudEvent
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = nameof(GithubFlowEventType.ReadmeCreated),
                        Subject = item.Subject,
                        Data = new Dictionary<string, string> {
                                { "org", data["org"].ToString() },
                                { "repo", data["repo"].ToString() },
                                { "issueNumber", data["issueNumber"].ToString() },
                                { "readme", lastReadme },
                                { "parentNumber", data["parentNumber"].ToString() }
                            },
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
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask) };
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf", context);
            return await CallFunction(PMSkills.Readme, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating readme");
            return default;
        }
    }
}

public interface IManageProducts : IActor
{
    public Task<string> CreateReadme(string ask);
}

public class ProductManagerState
{
    public string Capabilities { get; set; }
}