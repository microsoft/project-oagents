using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Writer : AiAgent<WriterState>, IWriter
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<GraphicDesigner> _logger;

    public Writer([PersistentState("state", "messages")] IPersistentState<AgentState<WriterState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<GraphicDesigner> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserConnected):
                // The user reconnected, let's send the last message if we have one
                if(_state.State.History?.Last().Message == null)
                {                     
                    return;
                }
                var lastMessage = _state.State.History.Last().Message;

                // TODO
                // send a meaningful message to the user

                break;

            case nameof(EventTypes.UserChatInput):                
                //var lastCode = _state.State.History.Last().Message;

                _logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.UserChatInput)}. UserMessage: {item.Message}");
                    
                var context = new KernelArguments { ["input"] = AppendChatHistory(item.Message) };
                string newArticle = await CallFunction(WriterPrompts.Write, context);
                _state.State.Data.WrittenArticle = newArticle;


                await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
                {
                    Type = nameof(EventTypes.ArticleWritten),
                    Data = new Dictionary<string, string> {
                            { "UserId", item.Data["UserId"] },
                            { "UserMessage", item.Data["userMessage"] },
                            { "WrittenArticle", newArticle }
                        },
                    Message = newArticle
                });



                break;
            default:
                break;
        }
    }

    public Task<String> GetArticle()
    {
        return Task.FromResult(_state.State.Data.WrittenArticle);
    }
}

public interface IWriter : IGrainWithStringKey
{
    Task<String> GetArticle();
}

[GenerateSerializer]
public class WriterState
{
    [Id(0)]
    public string WrittenArticle { get; set; }
}