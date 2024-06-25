using SupportCenter.Extensions;
using SupportCenter.SignalRHub;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
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
            //.WithOrigins("http://localhost:3000", "http://localhost:3001") // client url
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
}

builder.Services.ExtendOptions();
builder.Services.ExtendServices();
builder.Services.RegisterSemanticKernelNativeFunctions();

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