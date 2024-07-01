using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Options;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Invoice : AiAgent<InvoiceState>
{
    private readonly ILogger<Invoice> _logger;

    protected override string Namespace => Consts.OrleansNamespace;
    protected override Kernel Kernel { get; }
    protected override ISemanticTextMemory Memory { get; }

    public Invoice([PersistentState("state", "messages")] IPersistentState<AgentState<InvoiceState>> state,
        ILogger<Invoice> logger,
        [FromKeyedServices("InvoiceKernel")] Kernel kernel,
        [FromKeyedServices("InvoiceMemory")] ISemanticTextMemory memory)
    : base(state)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    public async override Task HandleEvent(Event item)
    {

        switch (item.Type)
        {
            case nameof(EventType.InvoiceRequested):
                {
                    var userId = item.Data["userId"];
                    var userMessage = item.Data["userMessage"];
                    var context = new KernelArguments { ["input"] = userMessage ?? throw new ArgumentNullException(nameof(userMessage)) };

                    await SendAnswerEvent($"Please wait while I look up the details for invoice...", userId);
                    _logger.LogInformation($"[{nameof(Invoice)}] Event {nameof(EventType.InvoiceRequested)}. UserQuestion: {userMessage}");
                    var querycontext = new KernelArguments { ["input"] = AppendChatHistory(userMessage)};
                    var instruction = "Consider the following knowledge:!invoices!";
                    var enhancedContext = await AddKnowledge(instruction, "invoices", querycontext);
                    string answer = await CallFunction(InvoicePrompts.InvoiceRequest, enhancedContext);
                    await SendAnswerEvent(answer, userId);
                    break;
                }
            default:
                break;
        }
    }
    private async Task SendAnswerEvent(string message, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventType.InvoiceRetrieved),
            Data = new Dictionary<string, string> {
                { nameof(userId), userId },
                { nameof(message), message }
            }
        });
    }
}