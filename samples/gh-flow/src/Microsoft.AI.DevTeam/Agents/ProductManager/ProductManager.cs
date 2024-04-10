using CloudNative.CloudEvents;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class ProductManager : AiAgent<ProductManagerState>, IManageProducts
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly ILogger<ProductManager> _logger;

    public ProductManager([PersistentState("state", "messages")] IPersistentState<AgentState<ProductManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<ProductManager> logger) 
    : base(state, memory, kernel)
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
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new CloudEvent {
                     Type = nameof(GithubFlowEventType.ReadmeGenerated),
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
                var lastReadme = _state.State.History.Last().Message;
                await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new CloudEvent {
                     Type = nameof(GithubFlowEventType.ReadmeCreated),
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
            var context = new KernelArguments { ["input"] = AppendChatHistory(ask)};
            var instruction = "Consider the following architectural guidelines:!waf!";
            var enhancedContext = await AddKnowledge(instruction, "waf",context);
            return await CallFunction(PMSkills.Readme, enhancedContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating readme");
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