using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Octokit.Webhooks;
using Octokit.Webhooks.AzureFunctions;

namespace KernelHttpServer;

public static class Program
{
    public static void Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureGitHubWebhooks()
            .ConfigureAppConfiguration(configuration =>
            {
                var config = configuration.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables();

                var builtConfig = config.Build();
            })
            .ConfigureServices(services =>
            {
                services.AddTransient((provider) => CreateKernel(provider));
                services.AddScoped<GithubService>();
                services.AddScoped<WebhookEventProcessor, SKWebHookEventProcessor>();
                services.AddOptions<GithubOptions>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection("GithubOptions").Bind(settings);
                    });
                services.AddOptions<AzureOptions>()
                    .Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection("AzureOptions").Bind(settings);
                    });

                // return JSON with expected lowercase naming
                services.Configure<JsonSerializerOptions>(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });
            })
            .Build();

        host.Run();
    }

    private static IKernel CreateKernel(IServiceProvider provider)
    {
        var kernelSettings = KernelSettings.LoadSettings();

        var kernelConfig = new KernelConfig();

        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(kernelSettings.LogLevel ?? LogLevel.Warning)
                .AddConsole()
                .AddDebug();
        });

        // TODO: load the quadrant config from environment variables
        var memoryStore = new QdrantMemoryStore(new QdrantVectorDbClient("http://qdrant:6333", 1536));
        var embedingGeneration = new AzureTextEmbeddingGeneration(kernelSettings.EmbeddingDeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey);
        var semanticTextMemory = new SemanticTextMemory(memoryStore, embedingGeneration);

        return new KernelBuilder()
                            .WithLogger(loggerFactory.CreateLogger<IKernel>())
                            .WithAzureChatCompletionService(kernelSettings.DeploymentOrModelId, kernelSettings.Endpoint, kernelSettings.ApiKey, true, kernelSettings.ServiceId, true)
                            .WithMemory(semanticTextMemory)
                            .WithConfiguration(kernelConfig).Build();
    }
}