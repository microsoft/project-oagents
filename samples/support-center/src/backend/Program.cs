using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Orleans.Serialization;
using SupportCenter.Core;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SignalRHub;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient(CreateKernel);
builder.Services.AddTransient(CreateMemory);
builder.Services.AddTransient(CreateAISearchMemory);
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISignalRService, SignalRService>();

// Allow any CORS origin if in DEV
const string AllowDebugOriginPolicy = "AllowDebugOrigin";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowDebugOriginPolicy, builder =>
        {
            builder
            .WithOrigins("http://localhost:3000") // client url
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
}

builder.Services.ExtendOptions();
builder.Services.ExtendServices();
builder.Services.RegisterNativeFunctions();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering()
               .AddMemoryStreams("StreamProvider")
               .AddMemoryGrainStorage("PubSubStore")
               .AddMemoryGrainStorage("messages");
    siloBuilder.UseInMemoryReminderService();
    siloBuilder.UseDashboard(x => x.HostSelf = true);
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

WebApplication app = builder.Build();

app.UseRouting();
app.UseCors(AllowDebugOriginPolicy);
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Support Center API v1");
});

app.Map("/dashboard", x => x.UseOrleansDashboard());
app.MapHub<SupportCenterHub>("/supportcenterhub");
app.Run();

static ISemanticTextMemory CreateMemory(IServiceProvider provider)
{
    OpenAIOptions openAiConfig = provider.GetService<IOptions<OpenAIOptions>>()?.Value ?? new OpenAIOptions();
    QdrantOptions qdrantConfig = provider.GetService<IOptions<QdrantOptions>>()?.Value ?? new QdrantOptions();

    openAiConfig.ValidateRequiredProperties();
    qdrantConfig.ValidateRequiredProperties();

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddConsole()
            .AddDebug();
    });

    var memoryBuilder = new MemoryBuilder();
    return memoryBuilder.WithLoggerFactory(loggerFactory)
                 .WithQdrantMemoryStore(qdrantConfig.Endpoint, qdrantConfig.VectorSize)
                 .WithAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAiConfig.EmbeddingsEndpoint, openAiConfig.EmbeddingsApiKey)
                 .Build();
}

static ISemanticTextMemory CreateAISearchMemory(IServiceProvider provider)
{
    AISearchOptions aiSearchConfig = provider.GetService<IOptions<AISearchOptions>>()?.Value ?? new AISearchOptions();

    aiSearchConfig.ValidateRequiredProperties();

    var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder
            .SetMinimumLevel(LogLevel.Debug)
            .AddConsole()
            .AddDebug();
    });

    var memoryBuilder = new MemoryBuilder();
    return memoryBuilder.WithLoggerFactory(loggerFactory)
                    .WithMemoryStore(new AzureAISearchMemoryStore(aiSearchConfig.SearchEndpoint, aiSearchConfig.SearchKey))
                    .WithAzureOpenAITextEmbeddingGeneration(aiSearchConfig.SearchEmbeddingDeploymentOrModelId, aiSearchConfig.SearchEmbeddingEndpoint, aiSearchConfig.SearchEmbeddingApiKey)
                    .Build();
}

static Kernel CreateKernel(IServiceProvider provider)
{
    OpenAIOptions openAiConfig = provider.GetService<IOptions<OpenAIOptions>>()?.Value ?? new OpenAIOptions();
    var clientOptions = new OpenAIClientOptions();
    clientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(5);
    var builder = Kernel.CreateBuilder();
    builder.Services.AddLogging(c => c.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));

    // Chat
    var openAIClient = new OpenAIClient(new Uri(openAiConfig.ChatEndpoint), new AzureKeyCredential(openAiConfig.ChatApiKey), clientOptions);
    if (openAiConfig.ChatEndpoint.Contains(".azure", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddAzureOpenAIChatCompletion(openAiConfig.ChatDeploymentOrModelId, openAIClient);
    }
    else
    {
        builder.Services.AddOpenAIChatCompletion(openAiConfig.ChatDeploymentOrModelId, openAIClient);
    }

    // Text to Image
    /*openAIClient = new OpenAIClient(new Uri(openAiConfig.ImageEndpoint), new AzureKeyCredential(openAiConfig.ImageApiKey), clientOptions);
    if (openAiConfig.ImageEndpoint.Contains(".azure", StringComparison.OrdinalIgnoreCase))
    {
        ArgumentException.ThrowIfNullOrEmpty(openAiConfig.ImageDeploymentOrModelId, nameof(openAiConfig.ImageDeploymentOrModelId));
        builder.Services.AddAzureOpenAITextToImage(openAiConfig.ImageDeploymentOrModelId, openAIClient);
    }
    else
    {
        builder.Services.AddOpenAITextToImage(openAiConfig.ImageApiKey);
    }*/

    // Embeddings
    openAIClient = new OpenAIClient(new Uri(openAiConfig.EmbeddingsEndpoint), new AzureKeyCredential(openAiConfig.EmbeddingsApiKey), clientOptions);
    if (openAiConfig.EmbeddingsEndpoint.Contains(".azure", StringComparison.OrdinalIgnoreCase))
    {
        builder.Services.AddAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAIClient);
    }
    else
    {
        builder.Services.AddOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAIClient);
    }

    builder.Services.ConfigureHttpClientDefaults(c =>
    {
        c.AddStandardResilienceHandler().Configure(o =>
        {
            o.Retry.MaxRetryAttempts = 5;
            o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        });
    });
    return builder.Build();
}