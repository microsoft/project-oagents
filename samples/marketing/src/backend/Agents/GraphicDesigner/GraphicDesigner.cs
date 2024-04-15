#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
using Marketing.Agents.GraphicDesigner;
using Marketing.Hubs;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class GraphicDesigner : AiAgent<GraphicDesignerState>, IGraphicDesigner
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<GraphicDesigner> _logger;
    private readonly IConfiguration _configuration;
    public GraphicDesigner([PersistentState("state", "messages")] IPersistentState<AgentState<GraphicDesignerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<GraphicDesigner> logger, IConfiguration configuration) 
    : base(state, memory, kernel)
    {
        _logger = logger;
        _configuration = configuration;
        if (state.State.Data == null)
        {
            state.State.Data = new GraphicDesignerState();
        }
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.ArticleWritten):                
                //var lastCode = _state.State.History.Last().Message;

                _logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleWritten)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };

                var openAIClient = new GraphicDesignerOpenAIClient(_logger, _configuration);
                var imageUrl = await openAIClient.GenerateImage(item.Message);

                string uri = imageUrl.AbsoluteUri;
                _state.State.Data.imageUrl = uri;

                ArticleHub._allHubs.TryGetValue(item.Data["UserId"], out var articleHub);
                articleHub.SendMessageToSpecificClient(item.Data["UserId"], uri, AgentTypes.GraphicDesigner);

                //await AddKnowledge(instruction, "waf", context);

                //await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
                //{
                //    Type = nameof(EventTypes.ArticleWritten),
                //    Data = new Dictionary<string, string> {
                //            { "org", item.Data["org"] },
                //            { "repo", item.Data["repo"] },
                //            { "issueNumber", item.Data["issueNumber"] },
                //            { "code", lastCode },
                //            { "parentNumber", item.Data["parentNumber"] }
                //        },
                //    Message = lastCode
                //});
                break;
            default:
                break;
        }
    }

    public Task<String> GetPicture()
    {
        return Task.FromResult(_state.State.Data.imageUrl);
    }
}

public interface IGraphicDesigner : IGrainWithStringKey
{
    Task<String> GetPicture();
}

[GenerateSerializer]
public class GraphicDesignerState
{
    [Id(0)]
    public string imageUrl { get; set; }
}
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task