var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AH_TestU4IDS_NET10_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AH_TestU4IDS_NET10_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.AH_TestU4IDS_NET10_ParentAPI>("ah-testu4ids-net10-parentapi")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
