using Azure;
using Azure.AI.OpenAI;
using Microsoft.AI.DevTeam;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using System.Text;

namespace Marketing.Agents.GraphicDesigner
{
    public class GraphicDesignerOpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly string _apiKey;
        private readonly string _apiEndpoint;

        public GraphicDesignerOpenAIClient(ILogger logger, IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _apiKey = configuration["OAIOptionsImages:ApiKey"];
            _apiEndpoint = configuration["OAIOptionsImages:ApiEndpoint"];
        }

        public async Task<Uri> GenerateImage(string prompt)
        {

            OpenAIClient client = new(new Uri(_apiEndpoint), new AzureKeyCredential(_apiKey));

            Response<ImageGenerations> imageGenerations = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    Prompt = GraphicDesignerPrompts.GenerateImage.Replace("{{$input}}", prompt).Substring(1,999),
                    Size = ImageSize.Size1024x1024,
                    ImageCount = 1,
                    DeploymentName = "dall-e-3"
                });

            // Image Generations responses provide URLs you can use to retrieve requested images
            return imageGenerations.Value.Data[0].Url;
        }   
    }
}
