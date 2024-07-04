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
public class Conversation : AiAgent<ConversationState>
{
    private readonly ILogger<Conversation> _logger;

    protected override string Namespace => Consts.OrleansNamespace;

    public Conversation([PersistentState("state", "messages")] IPersistentState<AgentState<ConversationState>> state,
        ILogger<Conversation> logger,
        [FromKeyedServices("ConversationKernel")] Kernel kernel,
        [FromKeyedServices("ConversationMemory")] ISemanticTextMemory memory)
    : base(state, memory, kernel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            case nameof(EventType.ConversationRequested):
                _logger.LogInformation($"[{nameof(Conversation)}] Event {nameof(EventType.ConversationRequested)}. UserQuestion: {userMessage}");
                var context = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
                string answer = await CallFunction(ConversationPrompts.Answer, context);

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