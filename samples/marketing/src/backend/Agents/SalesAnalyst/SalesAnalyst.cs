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
public class SalesAnalyst : AiAgent<SalesAnalystState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<SalesAnalyst> _logger;

    public SalesAnalyst([PersistentState("state", "messages")] IPersistentState<AgentState<SalesAnalystState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<SalesAnalyst> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async Task AnalizeCampaign(string campaingText, string sessionId)
    {

        string prompt = SalesAnalystPrompt.ExtractDiscountFromCampaing.Replace("{{$input}}", campaingText);
        string response = await CallLlm(prompt);

        if (!Int32.TryParse(response, out int discountPercentage))
        {
            return;
        }


        int sales = CalculateSalesBasedOnDiscount(discountPercentage);


        if (discountPercentage > 10)
        {
            await SendSalesForecastEvent($"With a discount of {discountPercentage} we expect to sell {sales}", sessionId);
        }
        
    }



    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.CampaignCreated):
            {
                string text = item.Data["text"];
                _logger.LogInformation($"[{nameof(SalesAnalyst)}] Event {nameof(EventTypes.CampaignCreated)}. Text: {text}");

                AnalizeCampaign(text, item.Data["SessionId"]);

                //var context = new KernelArguments { ["input"] = AppendChatHistory(text) };
                //string auditorAnswer = await CallFunction(SalesAnalyst.AuditText, context);
                //if (auditorAnswer.Contains("NOTFORME"))
                //{
                //    return;
                //}
                break;
            }
            default:
                break;
        }
    }

    private async Task<string> CallLlm (string prompt)
    {
        var context = new KernelArguments { ["input"] = AppendChatHistory(prompt) };
        string answer = await CallFunction(prompt, context);
        return answer;
    }

    private async Task SendSalesForecastEvent(string auditorAlertMessage, string sessionId)
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


    private int CalculateSalesBasedOnDiscount(int discountPercentage)
    {
        return discountPercentage * 10000;
    }

}