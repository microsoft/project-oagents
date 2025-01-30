var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var redis = builder.AddRedis("cache");

var signalr = builder.AddAzureSignalR("signalr")
                     .RunAsEmulator();

var orleans = builder.AddOrleans("default")
                     .WithClustering(redis)
                     .WithStreaming(redis)
                     .WithGrainDirectory(redis)
                     .WithGrainStorage("PubSubStore", redis)
                     .WithGrainStorage("messages", redis);


var cosmos = builder.AddAzureCosmosDB("cosmos-db")
                    .WithDatabase("supportcenter")
                    .RunAsEmulator();

var openAi = builder.AddAzureOpenAI("openai");

var search = builder.AddAzureSearch("search");

var qdrant = builder.AddQdrant("qdrant");

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(orleans)
                        .WithReference(cosmos)
                        .WithReference(openAi)
                        .WithReference(search)
                        .WithReference(qdrant)
                        .WithReference(signalr)
                        .PublishAsAzureContainerApp((infra, capp) => { });

builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "local")
.WithReference(apiService)
    .WithEnvironment("VITE_OAGENT_BASE_URL", apiService.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var memorySeeder = builder.AddProject<Projects.SupportCenter_Seed_Memory>("memoryseeder")
                        .WithReference(redis);

var invoiceSeeder = builder.AddProject<Projects.SupportCenter_Seed_InvoiceMemory>("invoiceseeder")
                        .WithReference(redis);

builder.Build().Run();