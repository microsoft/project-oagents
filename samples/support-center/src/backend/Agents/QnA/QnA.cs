using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;

namespace SupportCenter.Agents;
[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class QnA : AiAgent<QnAState>
{
    private readonly ILogger<QnA> _logger;

    protected override string Namespace => Consts.OrleansNamespace;

    public QnA([PersistentState("state", "messages")] IPersistentState<AgentState<QnAState>> state,
        ILogger<QnA> logger,
        [FromKeyedServices("QnAKernel")] Kernel kernel,
        [FromKeyedServices("QnAMemory")] ISemanticTextMemory memory)
    : base(state, memory, kernel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }                
                break;
            case nameof(EventType.QnARequested):
                var ssc = item.GetAgentData();
                string? userId = ssc.UserId;
                string? message = ssc.UserMessage;
                string? id = ssc.Id;

                _logger.LogInformation($"userId: {userId}, message: {message}");
                if (userId == null || message == null)
                {
                    _logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }

                _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(QnA), nameof(EventType.QnARequested), message);
                await SendAnswerEvent(id, userId, $"Please wait while I look in the documents for answers to your question...");

                var context = new KernelArguments { ["input"] = AppendChatHistory(message) };
                var instruction = "Consider the following knowledge:!vfcon106047!";
                var enhancedContext = await AddKnowledge(instruction, "vfcon106047", context);
                string answer = await CallFunction(QnAPrompts.Answer, enhancedContext);

                await SendAnswerEvent(id, userId, answer);
                break;

            default:
                break;
        }
    }

    private async Task SendAnswerEvent(string id, string userId, string message)
    {
        await PublishEvent(Consts.OrleansNamespace, id, new Event
        {
            Type = nameof(EventType.QnARetrieved),
            Data = new Dictionary<string, string> {
                { nameof(userId), userId },
                { nameof(message), message }
            }
        });
    }
}