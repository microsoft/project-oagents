using Marketing.Controller;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using static System.Net.Mime.MediaTypeNames;

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

    public override async Task HandleEvent(Event item)
    {

        ArgumentNullException.ThrowIfNull(item);
        switch (item.Type)
        {
            case nameof(EventTypes.AuditText):
                {
                    string text = item.Data["text"];
                    _logger.LogInformation($"[{nameof(Auditor)}] Event {nameof(EventTypes.AuditText)}. Text: {text}");

                    var context = new KernelArguments { ["input"] = AppendChatHistory(text) };
                    string auditorAnswer = await CallFunction(AuditorPrompts.AuditText, context);
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