var builder = DistributedApplication.CreateBuilder(args);

var parentApi = builder.AddProject<Projects.AH_TestU4IDS_NET10_ParentAPI>("parentapi")
    .WithHttpHealthCheck("/health");

var weatherApi = builder.AddProject<Projects.AH_TestU4IDS_NET10_WeatherApi>("weatherapi");

parentApi.WithReference(weatherApi)
    .WaitFor(weatherApi);

builder.AddProject<Projects.AH_TestU4IDS_NET10_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(parentApi)
    .WaitFor(parentApi);

builder.Build().Run();
