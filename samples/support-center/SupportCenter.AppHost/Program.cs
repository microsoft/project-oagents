using Azure.Provisioning;
using Azure.Provisioning.AppContainers;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

var redis = builder.AddRedis("redis")
                    .WithImage("redis/redis-stack-server")
                    .WithRedisCommander();
//.WithDataVolume(isReadOnly: false);
//.WithPersistence(interval: TimeSpan.FromMinutes(1), keysChangedThreshold: 10);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection").AddDeployment(new AzureOpenAIDeployment("gpt-4o-mini", "gpt-4o-mini", "2024-07-18"))
    : builder.AddConnectionString("openAiConnection");

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(openai)
                        .WithReference(signalr)
                        .WithReference(redis)
                        .WaitFor(signalr)
                        .WaitFor(redis)
                        .WaitFor(openai)
                        .WithExternalHttpEndpoints();

if (builder.ExecutionContext.IsPublishMode)
{
    var storage = builder.AddAzureStorage("storage").RunAsEmulator();
    var clusteringTable = storage.AddTables("clustering");
    var grainStorage = storage.AddBlobs("grain-state");
    var streamingQueue = storage.AddQueues("streaming");

    var orleans = builder.AddOrleans("default")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("PubSubStore", grainStorage)
                     .WithGrainStorage("messages", grainStorage)
                     .WithStreaming("AzureQueueProvider", streamingQueue);

    var insights = builder.AddAzureApplicationInsights("ServiceCenter");
    apiService.WithReference(clusteringTable)
                        .WithReference(grainStorage)
                        .WithReference(insights)
                        .WithReference(streamingQueue)
                        .WaitFor(clusteringTable)
                        .WaitFor(grainStorage)
                        .WaitFor(streamingQueue)
                        .WaitFor(insights)
                        .WithEnvironment("HTTP_PORTS", "8081")
                        .WithReplicas(3)
                        .PublishAsAzureContainerApp((infra, capp) =>
                        {
                            capp.Configuration.Ingress.CorsPolicy = new ContainerAppCorsPolicy
                            {
                                AllowCredentials = true,
                                AllowedOrigins = new BicepList<string> { "https://*.azurecontainerapps.io" },
                                AllowedHeaders = new BicepList<string> { "*" },
                                AllowedMethods = new BicepList<string> { "*" },

                            };
                            capp.Configuration.Ingress.TargetPort = 8081;
                            capp.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
                        });
}
else
{
    var orleans = builder.AddOrleans("default")
                     .WithDevelopmentClustering()
                     .WithMemoryStreaming("AzureQueueProvider")
                     .WithMemoryGrainStorage("PubSubStore")
                     .WithMemoryGrainStorage("messages");
    apiService.WithReference(orleans);
}

builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "dev")
    .WithReference(apiService)
    .WithEnvironment("VITE_OAGENT_BASE_URL", apiService.GetEndpoint("http"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(apiService);

var memorySeeder = builder.AddProject<Projects.SupportCenter_Seed_Memory>("memoryseeder")
                         .WithReference(redis)
                         .WithReference(openai)
                         .WaitFor(redis)
                         .WaitFor(openai);

builder.Build().Run();