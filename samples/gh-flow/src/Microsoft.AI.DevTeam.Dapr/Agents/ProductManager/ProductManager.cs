using CloudNative.CloudEvents;
using Dapr.Actors.Runtime;
using Microsoft.AI.Agents.Dapr;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Microsoft.AI.DevTeam;

public class ProductManager : AiAgent<ProductManagerState>, IManageProducts
{
    protected override string Namespace => Consts.MainNamespace;
    private readonly ILogger<ProductManager> _logger;

    public ProductManager(ActorHost host, Kernel kernel, ISemanticTextMemory memory, ILogger<ProductManager> logger) 
    : base(host, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(CloudEvent item)
    {
        switch (item.Type)
        {
            case nameof(GithubFlowEventType.ReadmeRequested):
                // var readme = await CreateReadme(item.Message);
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                //      Type = nameof(GithubFlowEventType.ReadmeGenerated),
                //         Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             { "readme", readme }
                //         },
                //        Message = readme
                // });
                break;
            case nameof(GithubFlowEventType.ReadmeChainClosed):
                // var lastReadme = _state.State.History.Last().Message;
                // await PublishEvent(Consts.MainNamespace, this.GetPrimaryKeyString(), new Event {
                //      Type = nameof(GithubFlowEventType.ReadmeCreated),
                //         Data = new Dictionary<string, string> {
                //             { "org", item.Data["org"] },
                //             { "repo", item.Data["repo"] },
                //             { "issueNumber", item.Data["issueNumber"] },
                //             { "readme", lastReadme },
                //             { "parentNumber", item.Data["parentNumber"] }
                //         },
                //        Message = lastReadme
                // });
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

public class ProductManagerState
{
    public string Capabilities { get; set; }
}