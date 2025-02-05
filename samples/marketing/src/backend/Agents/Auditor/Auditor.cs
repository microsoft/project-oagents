using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Auditor([PersistentState("state", "messages")] IPersistentState<AgentState<AuditorState>> state, IChatClient chatClient, ILogger<Auditor> logger) : AiAgent<AuditorState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.AuditText):
                {
                    string text = item.Data["text"];
                    logger.LogInformation($"[{nameof(Auditor)}] Event {nameof(EventTypes.AuditText)}. Text: {text}");

                    var input = AppendChatHistory(text);
                    var prompt = $"""
                                    You are an Auditor in a Marketing team
                                    Audit the text bello and make sure we do not give discounts larger than 10%
                                    If the text talks about a larger than 10% discount, reply with a message to the user saying that the discount is too large, and by company policy we are not allowed.
                                    If the message says who wrote it, add that information in the response as well
                                    In any other case, reply with NOTFORME
                                    ---
                                    Input: {input}
                                    ---
                                    """;
                    var result = await chatClient.CompleteAsync(prompt);
                    var auditorAnswer = result.Message.Text!;
                    if (auditorAnswer.Contains("NOTFORME"))
                    {
                        return;
                    }
                    await SendAuditorAlertEvent(auditorAnswer, item.Data["UserId"]);
                    break;
                }
            default:
                break;
        }
    }

    private async Task SendAuditorAlertEvent(string auditorAlertMessage, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.AuditorAlert),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { nameof(auditorAlertMessage), auditorAlertMessage}
                        }
        });
    }
}