using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Options;

namespace SupportCenter.Agents;
[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class QnA : AiAgent<QnAState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<QnA> _logger;

    public QnA([PersistentState("state", "messages")] IPersistentState<AgentState<QnAState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<QnA> logger) 
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.UserQuestionReceived):
                {
                    var userQuestion = item.Data["userQuestion"];
                    _logger.LogInformation($"[{nameof(QnA)}] Event {nameof(EventTypes.UserQuestionReceived)}. UserQuestion: {userQuestion}");
                    
                    var context = new KernelArguments { ["input"] = AppendChatHistory(userQuestion)};
                    var instruction = "Consider the following knowledge:!vfcon106047!";
                    var enhancedContext = await AddKnowledge(instruction, "vfcon106047", context);
                    string answer = await CallFunction(QnAPrompts.Answer, enhancedContext);

                    await SendAnswerEvent(answer, item.Data["UserId"]);
                    break;
                }
            default:
                break;
        }
    }

    private async Task SendAnswerEvent(string answer, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.UserQuestionAnswered),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { "Answer", answer },
                        }
        });
    }

}