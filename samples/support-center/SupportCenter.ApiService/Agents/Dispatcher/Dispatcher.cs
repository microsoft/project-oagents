using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Attributes;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using System.Reflection;
using static SupportCenter.ApiService.Options.Consts;

namespace SupportCenter.ApiService.Agents.Dispatcher;

[ImplicitStreamSubscription(OrleansNamespace)]
[DispatcherChoice("QnA", "The customer is asking a question related to internal Contoso knowledge base.", EventType.QnARequested)]
[DispatcherChoice("Discount", "The customer is asking for a discount about a product or service.", EventType.DiscountRequested)]
[DispatcherChoice("Invoice", "The customer is asking for an invoice.", EventType.InvoiceRequested)]
[DispatcherChoice("CustomerInfo", "The customer is asking for reading or updating his or her personal data.", EventType.CustomerInfoRequested)]
[DispatcherChoice("Conversation", "The customer is having a generic conversation. When the request is generic or can't be classified differently, use this choice.", EventType.ConversationRequested)]
public class Dispatcher(
        ILogger<Dispatcher> logger,
        [PersistentState("state", "messages")] IPersistentState<AgentState<DispatcherState>> state,
        [FromKeyedServices(Gpt4oMini)] IChatClient chatClient) : AiAgent<DispatcherState>(state)
{
    protected override string Namespace => OrleansNamespace;
    public async override Task HandleEvent(Event item)
    {
        logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Dispatcher), item.Type, item.Data);

        var ssc = item.GetAgentData();
        string? userId = ssc.UserId;
        string? message = ssc.UserMessage;
        string? id = ssc.Id;
        string? intent;

        logger.LogInformation($"userId: {userId}, message: {message}");
        if (userId == null || message == null)
        {
            logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
            return;
        }

        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one.
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]. Last message is missing.", nameof(Dispatcher), item.Type, item.Data);
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
                    logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Answer from agent is missing.", nameof(Dispatcher), item.Type, item.Data);
                    return;
                }
                logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Dispatcher), item.Type, answer);
                AddToHistory(answer, ChatUserType.Agent);
                break;
            default:
                break;
        }
    }

    private async Task<string> ExtractIntentAsync(string message)
    {
        var input = AppendChatHistory(message);
        var choices = GetAndSerializeChoices();
        var prompt = $"""
                    You are a dispatcher agent, working with the Support Center.
                    You can help customers with their issues, and you can also assign tickets to other AI agents.
                    Read the customer's message carefully, and then decide the appropriate intent.
                    A history of the conversation is available to help you make a decision.

                    If you don't know the intent, don't guess; instead respond with "Unknown".
                    There may be multiple intents, but you should choose the most appropriate one.
                    If you think that the message is not clear, you can ask the customer for more information.

                    You can choose between the following intents:  
                    {choices}  

                    Here are few examples:
                    - User Input: Can you help me in updating my address?
                    - CustomerInfo

                    - User Input: Could you check whether my invoice has been correctly payed?
                    - Invoice

                    Here is the user input:
                    User Input: {input}

                    Return the intent as a string.
                    """;
        var result = await chatClient.CompleteAsync(prompt);
        return result.Message.Text!;
    }

    // TODO: Custom attributes should be constructed only once in the lifetime of the application.
    private string GetAndSerializeChoices()
    {
        var choices = this.GetType().GetCustomAttributes<DispatcherChoice>()
            .Select(attr => new Choice(attr.Name, attr.Description))
            .ToArray();

        return string.Join("\n", choices.Select(c => $"- {c.Name}: {c.Description}")); ;
    }

    private async Task SendDispatcherEvent(string id, string userId, string intent, string message)
    {
        // TODO: Custom attributes should be constructed only once in the lifetime of the application.
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
