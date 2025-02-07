var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var eventHubs = builder.AddAzureEventHubs("eventHubsConnectionName")
                       .RunAsEmulator()
                       .WithHub("hub", config => config.ConsumerGroups.Add(new("orleansGroup")));

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var clusteringTable = storage.AddTables("clustering");
var snapshotTable = storage.AddTables("snapshot");
var grainStorage = storage.AddBlobs("grain-state");

var signalr = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureSignalR("signalr")
    : builder.AddConnectionString("signalr");

var redis = builder.AddRedis("redis")
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
                        .PublishAsAzureContainerApp((infra, capp) => { })
                        .WaitFor(eventHubs)
                        .WaitFor(signalr)
                        .WaitFor(redis)
                        .WaitFor(grainStorage);

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

//var invoiceSeeder = builder.AddProject<Projects.SupportCenter_Seed_InvoiceMemory>("invoiceseeder")
//                        .WithReference(redis);

builder.Build().Run();