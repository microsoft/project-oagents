using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AI.DevTeam;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Orleans.Configuration;
using Orleans.Serialization;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebhookEventProcessor, GithubWebHookProcessor>();
builder.Services.AddTransient(CreateKernel);
builder.Services.AddTransient(CreateMemory);
builder.Services.AddHttpClient();


builder.Services.AddTransient(s =>
{
    var ghOptions = s.GetService<IOptions<GithubOptions>>();
    var logger = s.GetService<ILogger<GithubAuthService>>();
    var ghService = new GithubAuthService(ghOptions, logger);
    var client = ghService.GetGitHubClient();
    return client;
});

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddArmClient(default);
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

builder.Services.AddSingleton<GithubAuthService>();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddOptions<GithubOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(GithubOptions)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(AzureOptions)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<OpenAIOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(OpenAIOptions)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<QdrantOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(QdrantOptions)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ServiceOptions>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(nameof(ServiceOptions)).Bind(settings);
    })
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IManageAzure, AzureService>();
builder.Services.AddSingleton<IManageGithub, GithubService>();
builder.Services.AddSingleton<IAnalyzeCode, CodeAnalyzer>();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseDashboard(x => x.HostSelf = true);
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering()
               .AddMemoryStreams("StreamProvider")
               .AddMemoryGrainStorage("PubSubStore")
               .AddMemoryGrainStorage("messages");

    siloBuilder.UseInMemoryReminderService();
    siloBuilder.UseDashboard(x => x.HostSelf = true);

        siloBuilder.UseInMemoryReminderService();
    }
    else
    {
        var cosmosDbconnectionString = builder.Configuration.GetValue<string>("AzureOptions:CosmosConnectionString");
        siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "ai-dev-cluster";
            options.ServiceId = "ai-dev-cluster";
        });
        siloBuilder.Configure<SiloMessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromMinutes(3);
            options.SystemResponseTimeout = TimeSpan.FromMinutes(3);
        });
        siloBuilder.Configure<ClientMessagingOptions>(options =>
       {
           options.ResponseTimeout = TimeSpan.FromMinutes(3);
       });
        siloBuilder.UseCosmosClustering(o =>
            {
                o.ConfigureCosmosClient(cosmosDbconnectionString);
                o.ContainerName = "devteam";
                o.DatabaseName = "clustering";
                o.IsResourceCreationEnabled = true;
            });

        siloBuilder.UseCosmosReminderService(o =>
        {
            o.ConfigureCosmosClient(cosmosDbconnectionString);
            o.ContainerName = "devteam";
            o.DatabaseName = "reminders";
            o.IsResourceCreationEnabled = true;
        });
        siloBuilder.AddCosmosGrainStorage(
            name: "messages",
            configureOptions: o =>
            {
                o.ConfigureCosmosClient(cosmosDbconnectionString);
                o.ContainerName = "devteam";
                o.DatabaseName = "persistence";
                o.IsResourceCreationEnabled = true;
            });
         //TODO: replace with EventHub
         siloBuilder
               .AddMemoryStreams("StreamProvider")
               .AddMemoryGrainStorage("PubSubStore");
    }    
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});
var app = builder.Build();

app.UseRouting()
   .UseEndpoints(endpoints =>
{
    var ghOptions = app.Services.GetService<IOptions<GithubOptions>>().Value;
    endpoints.MapGitHubWebhooks(secret: ghOptions.WebhookSecret);
});

app.Map("/dashboard", x => x.UseOrleansDashboard());

app.Run();

static ISemanticTextMemory CreateMemory(IServiceProvider provider)
{
    var openAiConfig = provider.GetService<IOptions<OpenAIOptions>>().Value;
    var qdrantConfig = provider.GetService<IOptions<QdrantOptions>>().Value;

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
                 //.WithAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingDeploymentOrModelId, openAiConfig.Endpoint, openAiConfig.ApiKey)
                 .Build();
}

static Kernel CreateKernel(IServiceProvider provider)
{
    var openAiConfig = provider.GetService<IOptions<OpenAIOptions>>().Value;
    var clientOptions = new AzureOpenAIClientOptions();
    //clientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(5);
    var openAIClient = new AzureOpenAIClient(new Uri(openAiConfig.Endpoint), new AzureKeyCredential(openAiConfig.ApiKey), clientOptions);
    var builder = Kernel.CreateBuilder();
    builder.Services.AddLogging(c => c.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));
    builder.Services.AddAzureOpenAIChatCompletion(openAiConfig.DeploymentOrModelId, openAIClient);
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