using Microsoft.Extensions.AI;
using OpenAI;
using SupportCenter.ApiService.SignalRHub;
using System.Text.Json;
using Orleans.Serialization;
using static SupportCenter.ApiService.Consts;
using SupportCenter.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSignalR()
                .AddNamedAzureSignalR("signalr");

builder.Services.AddSingleton<ISignalRService, SignalRService>();
builder.Services.AddTransient<ICustomerRepository, CustomerRepository>();


builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureBlobClient("grain-state");
builder.AddKeyedAzureTableClient("snapshot");

builder.AddKeyedRedisDistributedCache("redis");

builder.AddAzureOpenAIClient("openAiConnection");

builder.Services.AddKeyedChatClient(Gpt4oMini, s => {
    var innerClient = s.GetRequiredService<OpenAIClient>().AsChatClient(Gpt4oMini);
    return new ChatClientBuilder(innerClient)
               .UseFunctionInvocation().Build();
    
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