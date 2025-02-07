using Azure.Provisioning.AppContainers;
using Azure.Provisioning;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var eventHubs = builder.AddAzureEventHubs("eventHubsConnectionName")
                       .RunAsEmulator()
                       .AddHub("hub")
                       .AddConsumerGroup("orleansGroup");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var clusteringTable = storage.AddTables("clustering");
var snapshotTable = storage.AddTables("snapshot");
var grainStorage = storage.AddBlobs("grain-state");

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

var passwordParam = builder.AddParameter("pass");

var redis = builder.AddRedis("redis", password: passwordParam)
                    .WithRedisCommander()
                    .WithDataVolume(isReadOnly: false)
                    .WithPersistence( interval: TimeSpan.FromMinutes(1), keysChangedThreshold:10);

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection")
    : builder.AddConnectionString("openAiConnection");

var orleans = builder.AddOrleans("default")
                     .WithClustering(clusteringTable)
                     .WithGrainStorage("PubSubStore", grainStorage)
                     .WithGrainStorage("messages", grainStorage);

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(orleans)
                        .WithReference(eventHubs)
                        .WithReference(clusteringTable)
                        .WithReference(snapshotTable)
                        .WithReference(grainStorage)
                        .WithReference(openai)
                        .WithReference(signalr)
                        .WithReference(redis)
                        .WaitFor(eventHubs)
                        .WaitFor(signalr)
                        .WaitFor(redis)
                        .WaitFor(grainStorage)
                        .WithExternalHttpEndpoints()
                        .PublishAsAzureContainerApp((infra, capp) => {
                            capp.Configuration.Ingress.CorsPolicy = new ContainerAppCorsPolicy
                            {
                                AllowCredentials = true,
                                AllowedOrigins = new BicepList<string> { "https://*.azurecontainerapps.io" },
                                AllowedHeaders = new BicepList<string> { "*" },
                                AllowedMethods = new BicepList<string> { "*" },
                            };
                            capp.Configuration.Ingress.StickySessionsAffinity = StickySessionAffinity.Sticky;
                        });

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