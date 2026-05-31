var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AH_TestU4IDS_NET10_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AH_TestU4IDS_NET10_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
