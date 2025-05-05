using Marketing.Events;
using Marketing.Options;
using Marketing.SignalRHub;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using System;
using System.Security.Policy;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class SignalR : Agent
{
    protected override string Namespace => Consts.OrleansNamespace;
    
    private readonly ILogger<SignalR> _logger;
    private readonly ISignalRService _signalRClient;

    public SignalR(ILogger<SignalR> logger, ISignalRService signalRClient)
    {
        _logger = logger;
        _signalRClient = signalRClient;
    }

    public async override Task HandleEvent(Event item)
    {
        int attempt = 2;
        while (attempt > 0)
        {
            try
            {
                switch (item.Type)
                {
                    case nameof(EventTypes.CampaignCreated):
                        var writenArticle = item.Data["article"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], writenArticle, AgentTypes.Writer);
                        break;

                    case nameof(EventTypes.GraphicDesignCreated):
                        var imageUrl = item.Data["imageUri"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], imageUrl, AgentTypes.GraphicDesigner);
                        break;

                    case nameof(EventTypes.SocialMediaPostCreated):
                        var post = item.Data["socialMediaPost"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], post, AgentTypes.CommunityManager);
                        break;
                    case nameof(EventTypes.AuditorAlert):
                        var auditorAlertMessage = item.Data["auditorAlertMessage"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], auditorAlertMessage, AgentTypes.Auditor);
                        break;
                    case nameof(EventTypes.SalesForecast):
                        var salesForecast = item.Data["salesForecastMessage"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], salesForecast, AgentTypes.SalesAnalyst);
                        break;
                    case nameof(EventTypes.ManufacturingForecast):
                        var manufactureForecastMessage = item.Data["manufactureForecastMessage"];
                        await _signalRClient.SendMessageToSpecificClient(item.Data["SessionId"], manufactureForecastMessage, AgentTypes.ManufacturingManager);
                        break;
                    default:
                        break;
                }
                attempt = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(SignalR)}] Error while sending message to client.");
                await Task.Delay(1000);
                attempt--;
            }
        }
    }
}