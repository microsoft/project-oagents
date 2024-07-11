using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Orleans.Runtime;
using SupportCenter.Attributes;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SignalRHub;
using System.Reflection;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
[DispatcherChoice("QnA", "The customer is asking a question.", EventType.QnARequested)]
[DispatcherChoice("Discount", "The customer is asking for a discount about a product or service.", EventType.DiscountRequested)]
[DispatcherChoice("Invoice", "The customer is asking for an invoice.", EventType.InvoiceRequested)]
[DispatcherChoice("CustomerInfo", "The customer is asking for reading or updating his or her personal data.", EventType.CustomerInfoRequested)]
[DispatcherChoice("Conversation", "The customer is having a generic conversation. When the request is generic or can't be classified differently, use this choice.", EventType.ConversationRequested)]
public class Dispatcher : AiAgent<DispatcherState>
{
    private readonly ILogger<Dispatcher> _logger;

    protected override string Namespace => Consts.OrleansNamespace;

    public Dispatcher(
        ILogger<Dispatcher> logger,
        [PersistentState("state", "messages")] IPersistentState<AgentState<DispatcherState>> state,
        [FromKeyedServices("DispatcherKernel")] Kernel kernel
       )
    : base(state, default, kernel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task HandleEvent(Event item)
    {
        _logger.LogInformation("[{Agent}]:{EventType}:{EventData}", nameof(Dispatcher), item.Type, item.Data);

        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");
        if (userId == null || userMessage == null)
        {
            _logger.LogWarning("[{Agent}]:{EventType}:{EventData}. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
            return;
        }

        string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
        string id = $"{userId}/{conversationId}";
        string? intent;

        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one.
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    _logger.LogInformation("[{Agent}]:{EventType}:{EventData}. Last message is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }
                intent = (await ExtractIntentAsync(lastMessage))?.Trim(' ', '\"', '.') ?? string.Empty;
                await SendDispatcherEvent(id, userId, intent, lastMessage);
                break;
            case nameof(EventType.UserNewConversation):
                // The user started a new conversation.
                _state.State.History.Clear();
                break;
            case nameof(EventType.UserChatInput):
                intent = (await ExtractIntentAsync(userMessage))?.Trim(' ', '\"', '.') ?? string.Empty;
                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.DispatcherNotification),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { "message",  $"The user request has been dispatched to the '{intent}' agent." }
                    }
                });

                await SendDispatcherEvent(id, userId, intent, userMessage);
                break;
            case nameof(EventType.QnARetrieved):
            case nameof(EventType.DiscountRetrieved):
            case nameof(EventType.InvoiceRetrieved):
            case nameof(EventType.CustomerInfoRetrieved):
                var message = item.Data.GetValueOrDefault<string>("message");
                if (message == null)
                {
                    _logger.LogWarning("[{Agent}]:{EventType}:{EventData}. Message is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }
                _logger.LogInformation("[{Agent}]:{EventType}:{EventData}", nameof(Dispatcher), item.Type, message);
                AddToHistory(message, ChatUserType.Agent);
                break;
            default:
                break;
        }
    }

    private async Task<string> ExtractIntentAsync(string message)
    {
        var input = AppendChatHistory(message);

        var context = new KernelArguments { ["input"] = input };
        context.Add("choices", GetAndSerializeChoices());
        return await CallFunction(DispatcherPrompts.GetIntent, context);
    }

    private string GetAndSerializeChoices()
    {
        var choices = this.GetType().GetCustomAttributes<DispatcherChoice>()
            .Select(attr => new Choice(attr.Name, attr.Description))
            .ToArray();

        return string.Join("\n", choices.Select(c => $"- {c.Name}: {c.Description}")); ;
    }

    private async Task SendDispatcherEvent(string id, string userId, string intent, string userMessage)
    {
        var type = this.GetType()
            .GetCustomAttributes<DispatcherChoice>()
            .FirstOrDefault(attr => attr.Name == intent)?
            .DispatchToEvent.ToString() ?? EventType.Unknown.ToString();

        await PublishEvent(Namespace, id, new Event
        {
            Type = type,
            Data = new Dictionary<string, string>
            {
                { nameof(userId), userId },
                { nameof(userMessage),  userMessage }
            }
        });
    }
}
