using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Orleans.Runtime;
using SupportCenter.Events;
using SupportCenter.Options;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Invoice : AiAgent<InvoiceState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<Invoice> _logger;

    public Invoice([PersistentState("state", "messages")] IPersistentState<AgentState<InvoiceState>> state,
        Kernel kernel, ISemanticTextMemory memory, ILogger<Invoice> logger)
    : base(state, new MemoryBuilder().WithMemoryStore(new AzureAISearchMemoryStore( "<SearchEndpoint>", "<SearchKey>"))
                        .WithAzureOpenAITextEmbeddingGeneration("<SearchEmbeddingDeploymentOrModelId>", "<SearchEmbeddingEndpoint>", "<SearchEmbeddingApiKey>")
                        .Build(), kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        
        switch (item.Type)
        {
            case nameof(EventType.InvoiceRequested):
                {
                    var userId = item.Data["userId"];
                    var userMessage = item.Data["userMessage"];
                    //Try and find the invoice id in the user message
                    var context = new KernelArguments { ["input"] = userMessage };
                    var invoiceId = await CallFunction(InvoicePrompts.ExtractInvoiceId, context);
                    if (invoiceId == "Unknown")
                    {
                        AppendChatHistory(userMessage);
                        string requestForId = "Can you please provide the invoice id?";
                        AppendChatHistory(requestForId);
                        await SendAnswerEvent(requestForId, userId);
                        return;
                    }
                    await SendAnswerEvent($"Please wait while I look up the details for invoice {invoiceId} ...", userId);
                    /*var prompt = InvoicePrompts.InvoiceRequest
                        .Replace("{{$invoiceId}}", invoiceId)
                        .Replace("{{$userMessage}}", userMessage);*/
                    //TODO: We need to make sure an invoice belongs to the user before we can provide the information. Do we add metatdata, do we separate storage etc?
                    _logger.LogInformation($"[{nameof(Invoice)}] Event {nameof(EventType.InvoiceRequested)}. UserQuestion: {userMessage}");

                    var querycontext = new KernelArguments { ["input"] = AppendChatHistory(userMessage) };
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