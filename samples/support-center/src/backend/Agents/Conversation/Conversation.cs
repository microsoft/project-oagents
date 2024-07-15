using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SignalRHub;

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

                string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
                string id = $"{userId}/{conversationId}";
                _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Conversation), nameof(EventType.ConversationRequested), message);
                var context = new KernelArguments { ["input"] = AppendChatHistory(message) };
                string answer = await CallFunction(ConversationPrompts.Answer, context);

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