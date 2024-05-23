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
    private static string[] Choices => ["QnA", "Discount", "Invoice", "Customer Info"];

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
        switch (item.Type)
        {
            case nameof(EventTypes.UserChatInput):
                var userId = item.Data["UserId"];
                var userMessage = item.Data["userMessage"];

                var context = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
                context.Add("choices", Choices);
                string intent = await CallFunction(DispatcherPrompts.GetIntent, context);

                await SendDispatcherEvent(userId, intent);
                break;
            default:
                break;
        }
    }

    private async Task SendDispatcherEvent(string userId, string intent)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = intent switch
            {
                "QnA" => nameof(EventTypes.UserQuestionRequested),
                "Discount" => nameof(EventTypes.DiscountRequested),
                "Invoice" => nameof(EventTypes.InvoiceRequested),
                "Customer Info" => nameof(EventTypes.CustomerInfoRequested),
                _ => nameof(EventTypes.Unknown)
            },
            Data = new Dictionary<string, string>
            {
                { "UserId", userId }
            }
        });
    }
}