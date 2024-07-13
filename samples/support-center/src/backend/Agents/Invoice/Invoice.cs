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
public class Invoice : AiAgent<InvoiceState>
{
    private readonly ILogger<Invoice> _logger;

    protected override string Namespace => Consts.OrleansNamespace;

    public Invoice([PersistentState("state", "messages")] IPersistentState<AgentState<InvoiceState>> state,
        ILogger<Invoice> logger,
        [FromKeyedServices("InvoiceKernel")] Kernel kernel,
        [FromKeyedServices("InvoiceMemory")] ISemanticTextMemory memory)
    : base(state, memory, kernel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task HandleEvent(Event item)
    {
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");

        string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
        string id = $"{userId}/{conversationId}";

        switch (item.Type)
        {
            case nameof(EventType.InvoiceRequested):
                {
                    await SendAnswerEvent(id, userId, $"Please wait while I look up the details for invoice...");
                    _logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Invoice), nameof(EventType.InvoiceRequested), userMessage);

                    var querycontext = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
                    var instruction = "Consider the following knowledge:!invoices!";
                    var enhancedContext = await AddKnowledge(instruction, "invoices", querycontext);
                    string answer = await CallFunction(InvoicePrompts.InvoiceRequest, enhancedContext);
                    await SendAnswerEvent(id, userId, answer);
                    break;
                }
            default:
                break;
        }
    }

    private async Task SendAnswerEvent(string id, string userId, string message)
    {
        await PublishEvent(Namespace, id, new Event
        {
            Type = nameof(EventType.InvoiceRetrieved),
            Data = new Dictionary<string, string>
            {
                { nameof(userId), userId },
                { nameof(message),  message }
            }
        });
    }
}