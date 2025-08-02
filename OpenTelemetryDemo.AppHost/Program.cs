var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.OpenTelemetryDemo_Api>("api");

builder.Build().Run();
