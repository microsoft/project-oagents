using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.Options;

namespace SupportCenter.ApiService.Agents.Invoice;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Invoice([PersistentState("state", "messages")] IPersistentState<AgentState<InvoiceState>> state,
        ILogger<Invoice> logger,
        IChatClient chatClient) : AiAgent<InvoiceState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

    public async override Task HandleEvent(Event item)
    {
        var ssc = item.GetAgentData();
        string? userId = ssc.UserId;
        string? message = ssc.UserMessage;
        string? id = ssc.Id;

        logger.LogInformation($"userId: {userId}, message: {message}");
        if (userId == null || message == null)
        {
            logger.LogWarning("[{Agent}]:[{EventType}]:[{EventData}]. Input is missing.", nameof(Dispatcher), item.Type, item.Data);
            return;
        }

        switch (item.Type)
        {
            case nameof(EventType.InvoiceRequested):
                {
                    await SendAnswerEvent(id, userId, $"Please wait while I look up the details for invoice...");
                    logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(Invoice), nameof(EventType.InvoiceRequested), message);

                    var input =  AppendChatHistory(message);
                    var instruction = "Consider the following knowledge:!invoices!";
                    var invoices = await AddKnowledge(instruction, "invoices");
                    var prompt = $"""
                        You are a helpful customer support/service agent that answers questions about user invoices based on your knowledge.
                        Make sure that the invoice belongs to the specific user before providing the information. If needed, ask for the invoice id etc. 
                        Be polite and professional and answer briefly based on your knowledge ONLY.
                        
                        Input: {input}
                        {invoices}
                        """;
                    var result = await chatClient.CompleteAsync(prompt);
                    var answer = result.Message.Text!;
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