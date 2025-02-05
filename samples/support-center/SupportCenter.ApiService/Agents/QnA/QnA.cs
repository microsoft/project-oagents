using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.Options;

namespace SupportCenter.ApiService.Agents.QnA;
[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class QnA([PersistentState("state", "messages")] IPersistentState<AgentState<QnAState>> state,
        ILogger<QnA> logger,
        IChatClient chatClient) : AiAgent<QnAState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

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

                logger.LogInformation($"userId: {userId}, message: {message}");
                if (userId == null || message == null)
                {
                    logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }

                logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(QnA), nameof(EventType.QnARequested), message);
                await SendAnswerEvent(id, userId, $"Please wait while I look in the documents for answers to your question...");

                var input = AppendChatHistory(message);
                var instruction = "Consider the following knowledge:!vfcon106047!";
                var documents = await AddKnowledge(instruction, "vfcon106047");
                var prompt = $"""
                                You are a helpful customer support/service agent at Contoso Electronics. Be polite and professional and answer briefly based on your knowledge ONLY.
                                Input: {input}
                                {documents}
                                """;
                
                var result = await chatClient.CompleteAsync(prompt);
                var answer = result.Message.Text!;

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