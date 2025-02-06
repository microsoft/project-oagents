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

var cosmos = builder.AddAzureCosmosDB("cosmos-db")
                    .RunAsEmulator()
                    .AddCosmosDatabase("supportcenter")
                    .AddContainer("items","/id");

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection")
    : builder.AddConnectionString("openAiConnection");

var qdrant = builder.AddQdrant("qdrant");

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
                        .WithReference(cosmos)
                        .WithReference(openai)
                        .WithReference(qdrant)
                        .WithReference(signalr)
                        .PublishAsAzureContainerApp((infra, capp) => { })
                        .WaitFor(eventHubs)
                        .WaitFor(cosmos)
                        .WaitFor(signalr)
                        .WaitFor(qdrant)
                        .WaitFor(grainStorage);

builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "local")
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