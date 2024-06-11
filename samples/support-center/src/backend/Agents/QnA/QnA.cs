using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using static Microsoft.AI.Agents.Orleans.Resolvers;

namespace SupportCenter.Agents;
[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class QnA : AiAgent<QnAState>
{
    protected override string Namespace => Consts.OrleansNamespace;
    protected override string Name => nameof(QnA);

    private readonly ILogger<QnA> _logger;

    public QnA([PersistentState("state", "messages")] IPersistentState<AgentState<QnAState>> state,
        KernelResolver kernelResolver,
        SemanticTextMemoryResolver memoryResolver,
        ILogger<QnA> logger)
    : base(state, kernelResolver, memoryResolver)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        string? messageId = item.Data.GetValueOrDefault<string>("id");
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");

        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }
                //await SendDispatcherEvent(userId, lastMessage, item.Data["userId"]);
                break;
            case nameof(EventType.QnARequested):
                _logger.LogInformation($"[{nameof(QnA)}] Event {nameof(EventType.QnARequested)}. UserQuestion: {userMessage}");
                await SendAnswerEvent(messageId, userId, $"Please wait while I look up the details to answer your question ...");

                var context = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
                var instruction = "Consider the following knowledge:!vfcon106047!";
                var enhancedContext = await AddKnowledge(instruction, "vfcon106047", context);
                string answer = await CallFunction(QnAPrompts.Answer, enhancedContext);

                await SendAnswerEvent(messageId, userId, answer);
                break;

            default:
                break;
        }
    }

    private async Task SendAnswerEvent(string id, string userId, string message)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventType.QnARetrieved),
            Data = new Dictionary<string, string> {
                { nameof(id), id },
                { nameof(userId), userId },
                { nameof(message), message }
            }
        });
    }
}