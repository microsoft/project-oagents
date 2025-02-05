var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var redis = builder.AddRedis("redis");

var signalr = builder.AddAzureSignalR("signalr")
                     .RunAsEmulator();

var cosmos = builder.AddAzureCosmosDB("cosmos-db")
                    .WithDatabase("supportcenter")
                    .RunAsEmulator();

var openai = builder.ExecutionContext.IsPublishMode
    ? builder.AddAzureOpenAI("openAiConnection")
    : builder.AddConnectionString("openAiConnection");

var search = builder.AddAzureSearch("search");

var qdrant = builder.AddQdrant("qdrant");

var orleans = builder.AddOrleans("default")
                     .WithClustering(redis)
                     .WithGrainDirectory(redis)
                     .WithGrainStorage("PubSubStore", redis)
                     .WithGrainStorage("messages", redis);

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(orleans)
                        .WithReference(cosmos)
                        .WithReference(openai)
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