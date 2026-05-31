var builder = DistributedApplication.CreateBuilder(args);

var parentApi = builder.AddProject<Projects.AH_TestU4IDS_NET10_ParentAPI>("parentapi")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.AH_TestU4IDS_NET10_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(parentApi)
    .WaitFor(parentApi);

builder.Build().Run();
