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


        int salesExpectations = CalculateSalesBasedOnDiscount(discountPercentage);


        if (discountPercentage > 10)
        {
            await SendSalesForecastEvent($"With a discount of {discountPercentage}% we expect a sales increase of {salesExpectations} in the next 4 months", salesExpectations, sessionId);
        }
        
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.AuditorOk):
            {
                string article = item.Data["article"];
                _logger.LogInformation($"[{nameof(SalesAnalyst)}] Event {nameof(EventTypes.CampaignCreated)}. Text: {article}");

                AnalizeCampaign(article, item.Data["SessionId"]);

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

    private async Task SendSalesForecastEvent(string salesForecastMessage, int salesExpectations, string sessionId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.SalesForecast),
            Data = new Dictionary<string, string> {
                            { "SessionId", sessionId },
                            { nameof(salesForecastMessage), salesForecastMessage},
                            { nameof(salesExpectations) , salesExpectations.ToString() }
                        }
        });
    }


    private int CalculateSalesBasedOnDiscount(int discountPercentage)
    {
        return discountPercentage * 10000;
    }

}