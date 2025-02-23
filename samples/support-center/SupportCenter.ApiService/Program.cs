using Microsoft.Extensions.AI;
using OpenAI;
using SupportCenter.ApiService.SignalRHub;
using System.Text.Json;
using Orleans.Serialization;
using static SupportCenter.ApiService.Consts;
using SupportCenter.ApiService.Data;
using Microsoft.Extensions.VectorData;
using StackExchange.Redis;
using Microsoft.SemanticKernel.Connectors.Redis;
using OpenAI.Audio;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSignalR()
                .AddNamedAzureSignalR("signalr");

builder.Services.AddSingleton<ISignalRService, SignalRService>();
builder.Services.AddTransient<ICustomerRepository, CustomerRepository>();

builder.AddKeyedRedisDistributedCache("redis");
builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureTableClient("snapshot");
builder.AddKeyedAzureBlobClient("grain-state");
builder.AddKeyedAzureQueueClient("streaming");

builder.AddAzureOpenAIClient("openAiConnection");

builder.Services.AddKeyedChatClient(Gpt4oMini, s => {
    var innerClient = s.GetRequiredService<OpenAIClient>().AsChatClient(Gpt4oMini);
    return new ChatClientBuilder(innerClient)
               .UseFunctionInvocation().Build();
});

builder.Services.AddKeyedScoped<AudioClient>(Whisper, (s, o) => {
    return s.GetRequiredService<OpenAIClient>().GetAudioClient(Whisper);
});

builder.Services.AddSingleton<IVectorStore>(sp =>
{
    var db = sp.GetRequiredKeyedService<IConnectionMultiplexer>("redis").GetDatabase();
    return new RedisVectorStore(db, new() { StorageType = RedisStorageType.HashSet });
});

builder.Services.AddSingleton<IVectorRepository, VectorRepository>();

builder.Services.AddEmbeddingGenerator(s => {
    return s.GetRequiredService<OpenAIClient>().AsEmbeddingGenerator("text-embedding-3-large");
});
// Allow any CORS origin if in DEV
const string AllowDebugOriginPolicy = "AllowDebugOrigin";
const string AllowOriginPolicy = "AllowOrigin";
var isDev = builder.Environment.IsDevelopment();
if (isDev)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowDebugOriginPolicy, builder =>
        {
            builder.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
    });
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(AllowOriginPolicy, builder =>
        {
            builder
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .WithOrigins("https://*.azurecontainerapps.io") // client url
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
}

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.Services.AddSerializer(serializerBuilder =>
    {
        serializerBuilder.AddJsonSerializer(
        isSupported: type => true);
    });
});

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseRouting();
if (isDev)
{
    app.UseCors(AllowDebugOriginPolicy);
}
else
{
    app.UseCors(AllowOriginPolicy);

}
app.MapControllers();

app.MapHub<SupportCenterHub>("/supportcenterhub");

app.Run();
