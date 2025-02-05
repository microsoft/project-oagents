using Microsoft.Extensions.AI;
using OpenAI;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.SignalRHub;
using System.Text.Json;
using static SupportCenter.ApiService.Options.Consts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR()
                .AddNamedAzureSignalR("signalr");
builder.Services.AddSingleton<ISignalRService, SignalRService>();

builder.AddAzureCosmosClient(connectionName: "cosmos-db");
builder.AddRedisClient(connectionName: "redis");
builder.AddAzureOpenAIClient("openAiConnection");
builder.AddQdrantClient("qdrant");
builder.AddAzureSearchClient("searchConnectionName");

builder.Services.AddKeyedChatClient(Gpt4oMini, s => s.GetRequiredService<OpenAIClient>().AsChatClient(Gpt4oMini));
// Allow any CORS origin if in DEV
const string AllowDebugOriginPolicy = "AllowDebugOrigin";
const string AllowOriginPolicy = "AllowOrigin";
if (builder.Environment.IsDevelopment())
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

builder.Services.ExtendOptions();
builder.Services.ExtendServices();
builder.Services.RegisterSemanticKernelNativeFunctions();

builder.UseOrleans();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

WebApplication app = builder.Build();

app.UseRouting();
if (builder.Environment.IsDevelopment())
{
    app.UseCors(AllowDebugOriginPolicy);
}
else
{
    app.UseCors(AllowOriginPolicy);

}
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Support Center API v1");
});

app.Map("/dashboard", x => x.UseOrleansDashboard());
app.MapHub<SupportCenterHub>("/supportcenterhub");
app.Run();