using ABCRetailers.Models;
using Azure.Data.Tables;
using System.Text.Json;
using System.Text;

namespace ABCRetailers.Services
{
    /// <summary>
    /// Service that calls Azure Functions via HTTP instead of direct Azure Storage access
    /// This implements the required architecture: MVC App -> Functions -> Azure Storage
    /// </summary>
    public class FunctionsApiService : IFunctionsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionsApiService> _logger;
        private readonly string _functionsBaseUrl;

        public FunctionsApiService(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<FunctionsApiService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("FunctionsAPI");
            _logger = logger;
            _functionsBaseUrl = configuration["FunctionsApi:BaseUrl"] ?? "http://localhost:7071/api";
            
            _logger.LogInformation($"FunctionsApiService initialized with base URL: {_functionsBaseUrl}");
        }

        // Table Operations
        public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new()
        {
            var endpoint = GetEndpoint<T>();
            _logger.LogInformation($"GET {endpoint}");

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var entities = JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                }) ?? new List<T>();

                _logger.LogInformation($"Retrieved {entities.Count} {typeof(T).Name} entities from Functions API");
                return entities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Functions API: GET {endpoint}");
                throw;
            }
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var endpoint = $"{GetEndpoint<T>()}/{partitionKey}/{rowKey}";
            _logger.LogInformation($"GET {endpoint}");

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var entity = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Functions API: GET {endpoint}");
                throw;
            }
        }

        public async Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var endpoint = GetEndpoint<T>();
            _logger.LogInformation($"POST {endpoint}");

            try
            {
                var json = JsonSerializer.Serialize(entity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var createdEntity = JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                _logger.LogInformation($"Created {typeof(T).Name} entity via Functions API");
                return createdEntity ?? entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Functions API: POST {endpoint}");
                throw;
            }
        }

        public async Task<T> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity
        {
            var endpoint = GetEndpoint<T>();
            _logger.LogInformation($"PUT {endpoint}");

            try
            {
                var json = JsonSerializer.Serialize(entity);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var updatedEntity = JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                _logger.LogInformation($"Updated {typeof(T).Name} entity via Functions API");
                return updatedEntity ?? entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Functions API: PUT {endpoint}");
                throw;
            }
        }

        public async Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            var endpoint = $"{GetEndpoint<T>()}/{partitionKey}/{rowKey}";
            _logger.LogInformation($"DELETE {endpoint}");

            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation($"Deleted {typeof(T).Name} entity via Functions API");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Functions API: DELETE {endpoint}");
                throw;
            }
        }

        // Blob Operations
        public async Task<string> UploadImageAsync(IFormFile file, string containerName)
        {
            var endpoint = $"{_functionsBaseUrl}/blob/{containerName}";
            _logger.LogInformation($"POST {endpoint}");

            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);

                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ImageUploadResponse>(responseJson, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

                return result?.ImageUrl ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image via Functions API");
                throw;
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            var endpoint = $"{_functionsBaseUrl}/blob/{containerName}";
            _logger.LogInformation($"POST {endpoint}");

            try
            {
                using var fileStream = file.OpenReadStream();
                using var content = new StreamContent(fileStream);
                
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                return file.FileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file via Functions API");
                throw;
            }
        }

        public async Task DeleteBlobAsync(string blobName, string containerName)
        {
            var endpoint = $"{_functionsBaseUrl}/blob/{containerName}/{blobName}";
            _logger.LogInformation($"DELETE {endpoint}");

            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting blob via Functions API");
                throw;
            }
        }

        // Queue Operations
        public async Task SendMessageAsync(string queueName, string message)
        {
            // For orders, we send directly to the orders endpoint which puts it in the queue
            if (queueName == "order-notifications")
            {
                _logger.LogInformation("Sending order to Functions API (will be queued)");
                // The order creation already handles this in AddEntityAsync for Order type
                return;
            }

            _logger.LogInformation($"Queue operation for {queueName}: {message}");
            // Other queue operations can be implemented as needed
            await Task.CompletedTask;
        }

        public async Task<string?> ReceiveMessageAsync(string queueName)
        {
            _logger.LogInformation($"Receive message from queue: {queueName}");
            // Queue receive operations are typically handled by queue-triggered functions
            await Task.CompletedTask;
            return null;
        }

        // File Share Operations
        public async Task<string> UploadToFileShareAsync(IFormFile file, string shareName, string directoryName = "")
        {
            var directoryPath = string.IsNullOrEmpty(directoryName) ? "" : $"/{directoryName}";
            var endpoint = $"{_functionsBaseUrl}/fileshare/{shareName}{directoryPath}";
            _logger.LogInformation($"POST {endpoint}");

            try
            {
                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.FileName);
                
                var response = await _httpClient.PostAsync(endpoint, content);
                response.EnsureSuccessStatusCode();

                return file.FileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading to file share via Functions API");
                throw;
            }
        }

        public async Task<byte[]> DownloadFromFileShareAsync(string shareName, string fileName, string directoryName = "")
        {
            var directoryPath = string.IsNullOrEmpty(directoryName) ? "" : $"/{directoryName}";
            var endpoint = $"{_functionsBaseUrl}/fileshare/{shareName}{directoryPath}/{fileName}";
            _logger.LogInformation($"GET {endpoint}");

            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading from file share via Functions API");
                throw;
            }
        }

        // Helper Methods
        private string GetEndpoint<T>()
        {
            var typeName = typeof(T).Name;
            return typeName switch
            {
                nameof(Customer) => $"{_functionsBaseUrl}/table/Customers",
                nameof(Product) => $"{_functionsBaseUrl}/table/Products",
                nameof(Order) => $"{_functionsBaseUrl}/table/Orders",
                _ => $"{_functionsBaseUrl}/table/{typeName}s"
            };
        }

        private class ImageUploadResponse
        {
            public string ImageUrl { get; set; } = string.Empty;
        }
    }
}

