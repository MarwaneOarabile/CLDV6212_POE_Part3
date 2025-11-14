using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Add Azure Storage clients
        services.AddSingleton(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection not configured");
            return new TableServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection not configured");
            return new BlobServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection not configured");
            return new QueueServiceClient(connectionString);
        });

        services.AddSingleton(provider =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection")
                ?? throw new InvalidOperationException("AzureStorageConnection not configured");
            return new ShareServiceClient(connectionString);
        });

        services.AddLogging();
    })
    .Build();

host.Run();