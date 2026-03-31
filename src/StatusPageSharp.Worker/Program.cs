using StatusPageSharp.Infrastructure.DependencyInjection;
using StatusPageSharp.Worker;

var builder = Host.CreateApplicationBuilder(args);

InfrastructureServiceCollectionExtensions.AddStatusPageInfrastructureForWorker(
    builder.Services,
    builder.Configuration
);
builder.Services.AddHostedService<MonitorWorker>();

var host = builder.Build();
host.Run();
