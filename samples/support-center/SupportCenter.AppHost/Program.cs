var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var cache = builder.AddRedis("cache");
var orleans = builder.AddOrleans("default");

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice")
                        .WithReference(orleans)
                        .PublishAsAzureContainerApp((infra, capp) => { });

builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "dev")
.WithReference(apiService)
    .WithEnvironment("NEXT_PUBLIC_BACKEND_URI", apiService.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var memorySeeder = builder.AddProject<Projects.SupportCenter_Seed_Memory>("memoryseeder")
                        .WithReference(cache);

var invoiceSeeder = builder.AddProject<Projects.SupportCenter_Seed_InvoiceMemory>("invoiceseeder")
                        .WithReference(cache);

builder.Build().Run();
