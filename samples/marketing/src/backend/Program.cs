using System.Text.Json;
using Marketing.SignalRHub;
using Marketing.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISignalRService, SignalRService>();


// CORS is handled at the platform level (ex: Azure Container Apps)
const string AllowAllCorsOrigin = "AllowAllCorsOrigin";
builder.Services.AddCors(options =>
{
    options.AddPolicy(AllowAllCorsOrigin, builder =>
    {
        builder
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

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

var app = builder.Build();

app.UseCors(AllowAllCorsOrigin);

app.UseRouting();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});

app.Map("/dashboard", x => x.UseOrleansDashboard());
app.MapHub<ArticleHub>("/articlehub");
app.Run();