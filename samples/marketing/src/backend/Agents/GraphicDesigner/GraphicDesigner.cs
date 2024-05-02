#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
using Marketing.Agents.GraphicDesigner;
using Marketing.Hubs;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.AI.DevTeam.Events;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class GraphicDesigner : AiAgent<GraphicDesignerState>, IGraphicDesigner
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<GraphicDesigner> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISignalRClient _signalRClient;

    public GraphicDesigner([PersistentState("state", "messages")] IPersistentState<AgentState<GraphicDesignerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<GraphicDesigner> logger, IConfiguration configuration, ISignalRClient signalRClient) 
    : base(state, memory, kernel)
    {
        _logger = logger;
        _configuration = configuration;
        _signalRClient = signalRClient;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserConnected):
            case nameof(EventTypes.ArticleWritten):                
                //var lastCode = _state.State.History.Last().Message;

                _logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleWritten)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };

                var openAIClient = new GraphicDesignerOpenAIClient(_logger, _configuration);
                var imageUrl = await openAIClient.GenerateImage(item.Message);

                string uri = imageUrl.AbsoluteUri;
                _state.State.Data.imageUrl = uri;

                _signalRClient.SendMessageToSpecificClient(item.Data["UserId"], uri, AgentTypes.GraphicDesigner);
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