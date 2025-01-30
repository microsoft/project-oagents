using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Orleans.Runtime;
using SupportCenter.ApiService.Attributes;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.Options;
using System.Reflection;

namespace SupportCenter.ApiService.Agents.Dispatcher;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
[DispatcherChoice("QnA", "The customer is asking a question related to internal Contoso knowledge base.", EventType.QnARequested)]
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
        _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Dispatcher), item.Type, item.Data);

        var ssc = item.GetAgentData();
        string? userId = ssc.UserId;
        string? message = ssc.UserMessage;
        string? id = ssc.Id;
        string? intent;

        _logger.LogInformation($"userId: {userId}, message: {message}");
        if (userId == null || message == null)
        {
            _logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
            return;
        }

        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one.
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]. Last message is missing.", nameof(Dispatcher), item.Type, item.Data);
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
                intent = (await ExtractIntentAsync(message))?.Trim(' ', '\"', '.') ?? string.Empty;
                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.DispatcherNotification),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { nameof(message),  $"The user request has been dispatched to the '{intent}' agent." }
                    }
                });
                await SendDispatcherEvent(id, userId, intent, message);
                break;

            case nameof(EventType.QnARetrieved):
            case nameof(EventType.DiscountRetrieved):
            case nameof(EventType.InvoiceRetrieved):
            case nameof(EventType.CustomerInfoRetrieved):
            case nameof(EventType.ConversationRetrieved):
                var answer = item.Data.GetValueOrDefault<string>("message");
                if (answer == null)
                {
                    _logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Answer from agent is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }
                _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Dispatcher), item.Type, answer);
                AddToHistory(answer, ChatUserType.Agent);
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

    private async Task SendDispatcherEvent(string id, string userId, string intent, string message)
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
                { nameof(message),  message }
            }
        });
    }
}
