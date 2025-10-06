using ABCRetailers.Models;
using Azure.Data.Tables;

namespace ABCRetailers.Services
{
    /// <summary>
    /// Adapter that makes FunctionsApiService compatible with IAzureStorageService interface
    /// This allows controllers to use dependency injection with IAzureStorageService
    /// while the actual implementation calls Azure Functions
    /// </summary>
    public class FunctionsApiServiceAdapter : IAzureStorageService
    {
        private readonly IFunctionsApiService _functionsApiService;

        public FunctionsApiServiceAdapter(IFunctionsApiService functionsApiService)
        {
            _functionsApiService = functionsApiService;
        }

        // Table Operations - delegate to Functions API
        public Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
            => _functionsApiService.GetAllEntitiesAsync<T>();

        public Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
            => _functionsApiService.GetEntityAsync<T>(partitionKey, rowKey);

        public Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity
            => _functionsApiService.AddEntityAsync(entity);

        public Task<T> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
            => _functionsApiService.UpdateEntityAsync(entity);

        public Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
            => _functionsApiService.DeleteEntityAsync<T>(partitionKey, rowKey);

        // Blob Operations - delegate to Functions API
        public Task<string> UploadImageAsync(IFormFile file, string containerName)
            => _functionsApiService.UploadImageAsync(file, containerName);

        public Task<string> UploadFileAsync(IFormFile file, string containerName)
            => _functionsApiService.UploadFileAsync(file, containerName);

        public Task DeleteBlobAsync(string blobName, string containerName)
            => _functionsApiService.DeleteBlobAsync(blobName, containerName);

        // Queue Operations - delegate to Functions API
        public Task SendMessageAsync(string queueName, string message)
            => _functionsApiService.SendMessageAsync(queueName, message);

        public Task<string?> ReceiveMessageAsync(string queueName)
            => _functionsApiService.ReceiveMessageAsync(queueName);

        // File Share Operations - delegate to Functions API
        public Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
            => _functionsApiService.UploadToFileShareAsync(file, shareName, directoryName);

        public Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
            => _functionsApiService.DownloadFromFileShareAsync(shareName, fileName, directoryName);
    }
}


