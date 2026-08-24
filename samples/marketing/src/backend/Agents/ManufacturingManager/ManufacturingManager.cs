using Marketing.Controller;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using static System.Net.Mime.MediaTypeNames;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class ManufacturingManager : AiAgent<ManufacturingManagerState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<ManufacturingManager> _logger;

    public ManufacturingManager([PersistentState("state", "messages")] IPersistentState<AgentState<ManufacturingManagerState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<ManufacturingManager> logger)
    : base(state, memory, kernel)
    {
        _logger = logger;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.SalesForecast):
                string salesExpectations = item.Data["salesForecastMessage"];
                _logger.LogInformation($"[{nameof(ManufacturingManager)}] Event {nameof(EventTypes.SalesForecast)}. Text: {salesExpectations}");

                var context = new KernelArguments { ["input"] = AppendChatHistory(salesExpectations) };
                string manufactureManagerAnswer = await CallFunction(ManufacturingManagerPrompt.ManufacturingCreateProductionForecast, context);

                SendManufactureForecastEvent(manufactureManagerAnswer, item.Data["SessionId"]);
                break;
            default:
                break;
        }
    }
    private async Task SendManufactureForecastEvent(string manufactureForecastMessage, string sessionId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.ManufacturingForecast),
            Data = new Dictionary<string, string> {
                            { "SessionId", sessionId },
                            { nameof(manufactureForecastMessage), manufactureForecastMessage},
                        }
        });
    }

}