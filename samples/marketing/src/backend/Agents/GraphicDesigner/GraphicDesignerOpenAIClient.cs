using Azure;
using Azure.AI.OpenAI;

namespace Marketing.Agents;

public class GraphicDesignerOpenAIClient
{
    private readonly int MAX_PROMPT_LENGTH = 999;
    private readonly ILogger _logger;
    private readonly string _apiKey;
    private readonly string _apiEndpoint;

    public GraphicDesignerOpenAIClient(ILogger logger, IConfiguration configuration)
    {
        _logger = logger;
        _apiKey = configuration["OAIOptionsImages:ApiKey"];
        _apiEndpoint = configuration["OAIOptionsImages:ApiEndpoint"];
    }

    public async Task<Uri> GenerateImage(string prompt)
    {
        OpenAIClient client = new(new Uri(_apiEndpoint), new AzureKeyCredential(_apiKey));

        prompt = GraphicDesignerPrompts.GenerateImage.Replace("{{$input}}", prompt);
        prompt = prompt.Length > MAX_PROMPT_LENGTH ? prompt.Substring(0, MAX_PROMPT_LENGTH) : prompt;

        Response<ImageGenerations> imageGenerations = await client.GetImageGenerationsAsync(
            new ImageGenerationOptions()
            {
                Prompt = prompt,
                Size = ImageSize.Size1024x1024,
                ImageCount = 1,
                DeploymentName = "dall-e-3"
            });

        // Image Generations responses provide URLs you can use to retrieve requested images
        return imageGenerations.Value.Data[0].Url;
    }
}