using System.Text.Json;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.Memory.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Octokit;
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
                services.AddSingleton<IOpenApiConfigurationOptions>(_ => s_apiConfigOptions);
                services.AddTransient((provider) => CreateKernel(provider));
                services.AddScoped<WebhookEventProcessor, SKWebHookEventProcessor>();

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

    private static readonly OpenApiConfigurationOptions s_apiConfigOptions = new()
    {
        Info = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "Semantic Kernel Azure Functions Starter",
            Description = "Azure Functions starter application for the [Semantic Kernel](https://github.com/microsoft/semantic-kernel).",
            Contact = new OpenApiContact()
            {
                Name = "Issues",
                Url = new Uri("https://github.com/microsoft/semantic-kernel-starters/issues"),
            },
            License = new OpenApiLicense()
            {
                Name = "MIT",
                Url = new Uri("https://github.com/microsoft/semantic-kernel-starters/blob/main/LICENSE"),
            }
        },
        Servers = DefaultOpenApiConfigurationOptions.GetHostNames(),
        OpenApiVersion = OpenApiVersionType.V2,
        ForceHttps = false,
        ForceHttp = false,
    };
}
