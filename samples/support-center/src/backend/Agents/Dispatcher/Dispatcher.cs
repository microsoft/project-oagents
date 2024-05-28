using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Options;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Dispatcher : AiAgent<DispatcherState>
{
    protected override string Namespace => Consts.OrleansNamespace;
    private readonly ILogger<Dispatcher> _logger;

    private static Choice[] Choices => [
        new Choice("QnA", "The customer is asking a question. When the request is generic or can't be classified differently, use this choice."),
        new Choice("Discount", "The customer is asking for a discount about a product or service."),
        new Choice("Invoice", "The customer is asking for an invoice."),
        new Choice("Customer Info", "The customer is asking for reading or updating his or her information or profile.")
    ];

    public Dispatcher(
        [PersistentState("state", "messages")] IPersistentState<AgentState<DispatcherState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<Dispatcher> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        _logger.LogInformation("[{Dispatcher}] Event {EventType}. Data: {EventData}", nameof(Dispatcher), item.Type, item.Data);

        switch (item.Type)
        {
            case nameof(EventTypes.UserChatInput):
                var userId = item.Data["UserId"];
                var userMessage = item.Data["userMessage"];

                var context = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
                context.Add("choices", SerializeChoices(Choices));
                string intent = await CallFunction(DispatcherPrompts.GetIntent, context);

                await SendDispatcherEvent(userId, intent, userMessage);
                break;
            default:
                break;
        }
    }

    private static string SerializeChoices(Choice[] choices)
    {
        return string.Join("\n", choices.Select(c => $"- {c.Name}: {c.Description}"));
    }

    private async Task SendDispatcherEvent(string userId, string intent, string userMessage)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = intent switch
            {
                "QnA" => nameof(EventTypes.QnARequested),
                "Discount" => nameof(EventTypes.DiscountRequested),
                "Invoice" => nameof(EventTypes.InvoiceRequested),
                "Customer Info" => nameof(EventTypes.CustomerInfoRequested),
                _ => nameof(EventTypes.Unknown)
            },            
            Data = new Dictionary<string, string>
            {
                { "UserId", userId },
                { "userMessage", userMessage },
            }
        });
    }
}