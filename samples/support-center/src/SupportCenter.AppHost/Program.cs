var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureProvisioning();

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.SupportCenter_ApiService>("apiservice");

builder.AddNpmApp("frontend", "../SupportCenter.Frontend", "dev")
.WithReference(apiService)
    .WithEnvironment("NEXT_PUBLIC_BACKEND_URI", apiService.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
