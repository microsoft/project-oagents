using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Images;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class GraphicDesigner([PersistentState("state", "messages")] IPersistentState<AgentState<GraphicDesignerState>> state, OpenAIClient openAiClient, ILogger<GraphicDesigner> logger, IConfiguration configuration) : AiAgent<GraphicDesignerState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

    public async override Task HandleEvent(Event item)
    {
        string lastMessage;

        switch (item.Type)
        {
            case nameof(EventTypes.UserConnected):
                // The user reconnected, let's send the last message if we have one
                lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }

                await SendDesignedCreatedEvent(lastMessage, item.Data["UserId"]);

                break;
            case nameof(EventTypes.ArticleCreated):
                //TODO

                if (!String.IsNullOrEmpty(_state.State.Data.imageUrl))
                {
                    return;
                }

                logger.LogInformation($"[{nameof(GraphicDesigner)}] Event {nameof(EventTypes.ArticleCreated)}.");
                var article = item.Data["article"];

                var imageClient = openAiClient.GetImageClient("dall-e");
                var result = imageClient.GenerateImageAsync(article, new ImageGenerationOptions { Size = OpenAI.Images.GeneratedImageSize.W1024xH1024});

                var imageUri = ""; // TODO: Reimplement using OpenAI directly await dallEService.GenerateImageAsync(article, 1024, 1024);

                _state.State.Data.imageUrl = imageUri;

                await SendDesignedCreatedEvent(imageUri, item.Data["UserId"]);

                break;

            default:
                break;
        }
    }

    private async Task SendDesignedCreatedEvent(string imageUri, string userId)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.GraphicDesignCreated),
            Data = new Dictionary<string, string> {
                            { "UserId", userId },
                            { nameof(imageUri), imageUri}
                        }
        });
    }
}