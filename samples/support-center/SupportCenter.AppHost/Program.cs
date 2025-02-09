using Azure.Provisioning.AppContainers;
using Azure.Provisioning;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();



var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var clusteringTable = storage.AddTables("clustering");
var snapshotTable = storage.AddTables("snapshot");
var grainStorage = storage.AddBlobs("grain-state");
var streamingQueue = storage.AddQueues("streaming");

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

var passwordParam = builder.AddParameter("pass");

var redis = builder.AddRedis("redis", password: passwordParam)
                    .WithRedisCommander()
                    .WithDataVolume(isReadOnly: false)
                    .WithPersistence(interval: TimeSpan.FromMinutes(1), keysChangedThreshold: 10);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection").AddDeployment(new AzureOpenAIDeployment("gpt-4o-mini", "gpt-4o-mini", "2024-07-18"))
    : builder.AddConnectionString("openAiConnection");

var orleans = builder.ExecutionContext.IsPublishMode ?
              builder.AddOrleans("default")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("PubSubStore", grainStorage)
                     .WithGrainStorage("messages", grainStorage)
                     .WithMemoryStreaming("StreamProvider") :
              builder.AddOrleans("default")
                     .WithDevelopmentClustering()
                     .WithMemoryGrainStorage("PubSubStore")
                     .WithMemoryGrainStorage("messages")
                     .WithMemoryStreaming("StreamProvider");


var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(orleans)
                        .WithReference(openai)
                        .WithReference(signalr)
                        .WithReference(redis)
                        .WaitFor(signalr)
                        .WaitFor(redis)
                        .WaitFor(openai)
                        .WithExternalHttpEndpoints();
if (builder.ExecutionContext.IsPublishMode)
{
    var insights = builder.AddAzureApplicationInsights("ServiceCenter");
    apiService.WithReference(clusteringTable)
                        .WithReference(snapshotTable)
                        .WithReference(grainStorage)
                        .WithReference(streamingQueue)
                        .WithReference(insights)
                        .WaitFor(clusteringTable)
                        .WaitFor(snapshotTable)
                        .WaitFor(grainStorage)
                        .WaitFor(streamingQueue)
                        .WithEnvironment("HTTP_PORTS", "8081")
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
                        }); ;
}



builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "dev")
    .WithReference(apiService)
    .WithEnvironment("VITE_OAGENT_BASE_URL", apiService.GetEndpoint("http"))
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile()
    .WaitFor(apiService);

//var memorySeeder = builder.AddProject<Projects.SupportCenter_Seed_Memory>("memoryseeder")
//                        .WithReference(redis);

builder.Build().Run();