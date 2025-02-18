using System.Text.Json;
using Microsoft.AI.DevTeam;
using Microsoft.Extensions.Options;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebhookEventProcessor, GithubWebHookProcessor>();
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