using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") 
                             ?? Environment.GetEnvironmentVariable("AzureStorageConnection");
        
        services.AddSingleton(new TableServiceClient(connectionString));
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton(new QueueServiceClient(connectionString));
        services.AddSingleton(new ShareServiceClient(connectionString));
    })
    .Build();

host.Run();


