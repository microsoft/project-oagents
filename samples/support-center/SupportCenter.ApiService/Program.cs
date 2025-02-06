using Microsoft.Extensions.AI;
using OpenAI;
using SupportCenter.ApiService.SignalRHub;
using System.Text.Json;
using Orleans.Serialization;
using static SupportCenter.ApiService.Options.Consts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSignalR()
                .AddNamedAzureSignalR("signalr");
builder.Services.AddSingleton<ISignalRService, SignalRService>();

builder.AddAzureCosmosClient(connectionName: "cosmos-db");
builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureBlobClient("grain-state");
builder.AddKeyedAzureTableClient("snapshot");

builder.AddAzureOpenAIClient("openAiConnection");
builder.AddQdrantClient("qdrant");

builder.Services.AddKeyedChatClient(Gpt4oMini, s => s.GetRequiredService<OpenAIClient>().AsChatClient(Gpt4oMini));
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
            builder
            .WithOrigins("http://localhost:3000", "http://localhost:3001") // client urls
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

//builder.Services.ExtendOptions();
//builder.Services.ExtendServices();
//builder.Services.RegisterSemanticKernelNativeFunctions();

builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseDashboard(x => x.HostSelf = true);
    siloBuilder.Services.AddSerializer(serializerBuilder =>
    {
        serializerBuilder.AddJsonSerializer(
            isSupported: type => true);
    });

    siloBuilder.AddEventHubStreams("StreamProvider", (ISiloEventHubStreamConfigurator configurator) =>
    {
        //HACK: until Aspire fully wires up the streming provider
        var ehConnection = builder.Configuration["ConnectionStrings:eventHubsConnectionName"];
        
        configurator.ConfigureEventHub(b => b.Configure(options =>
        {
            options.ConfigureEventHubConnection(
                ehConnection,
                "hub",
                "orleansGroup");
        }));
        configurator.UseAzureTableCheckpointer(
            b => b.Configure((options) =>
            {
                options.TableServiceClient = new Azure.Data.Tables.TableServiceClient(builder.Configuration["ConnectionStrings:snapshot"]);
                options.PersistInterval = TimeSpan.FromSeconds(10);
            }));
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

app.Map("/dashboard", x => x.UseOrleansDashboard());
app.MapHub<SupportCenterHub>("/supportcenterhub");
app.Run();