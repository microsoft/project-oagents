using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.Options;
using SupportCenter.ApiService.SignalRHub;
using static SupportCenter.ApiService.Options.Consts;

namespace SupportCenter.ApiService.Agents.Conversation;
[ImplicitStreamSubscription(OrleansNamespace)]
public class Conversation([PersistentState("state", "messages")] IPersistentState<AgentState<ConversationState>> state,
        ILogger<Conversation> logger, [FromKeyedServices(Gpt4oMini)] IChatClient chatClient) : AiAgent<ConversationState>(state)
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
            case nameof(EventType.ConversationRequested):
                string? userId = item.Data.GetValueOrDefault<string>("userId");
                string? message = item.Data.GetValueOrDefault<string>("message");
                string? input = AppendChatHistory(message);

                string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
                string id = $"{userId}/{conversationId}";
                logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Conversation), nameof(EventType.ConversationRequested), message);

                var prompt = $""""
                    """
                    You are a helpful customer support/service agent at Contoso Electronics. Be polite, friendly and professional and answer briefly.
                    Answer with a plain string ONLY, without any extra words or characters like '.
                    Input: {input}
                    """";
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
            Type = nameof(EventType.ConversationRetrieved),
            Data = new Dictionary<string, string> {
                { nameof(id), id },
                { nameof(userId), userId },
                { nameof(message), message }
            }
        });
    }
}