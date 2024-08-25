using Marketing.Controller;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Auditor : AiAgent<AuditorState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<Auditor> _logger;

    public Auditor([PersistentState("state", "messages")] IPersistentState<AgentState<AuditorState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Auditor> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.CampaignCreated):
                string article = item.Data[nameof(article)];
                _logger.LogInformation($"[{nameof(Auditor)}] Event {nameof(EventTypes.CampaignCreated)}. {nameof(article)}: {article}");

                var context = new KernelArguments { ["input"] = AppendChatHistory(article) };
                string auditorAnswer = await CallFunction(AuditorPrompts.AuditText, context);
                if (auditorAnswer.Contains("NOTFORME"))
                {
                    return;
                }
                if (auditorAnswer.Contains("AUDITOK"))
                {
                    await SendAuditorOkEvent(auditorAnswer, article, item.Data["SessionId"]);

                }
                else
                {
                    await SendAuditorAlertEvent(auditorAnswer, item.Data["SessionId"]);
                }
                break;
            default:
                break;
        }
    }

    private async Task SendAuditorAlertEvent(string auditorAlertMessage, string sessionId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.AuditorAlert),
            Data = new Dictionary<string, string> {
                            { "SessionId", sessionId },
                            { nameof(auditorAlertMessage), auditorAlertMessage}
                        }
        });
    }

    private async Task SendAuditorOkEvent(string auditorOkMessage, string article, string sessionId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.AuditorOk),
            Data = new Dictionary<string, string> {
                            { "SessionId", sessionId },
                            { nameof(auditorOkMessage), auditorOkMessage},
                            { "article", article}
                        }
        });
    }
}