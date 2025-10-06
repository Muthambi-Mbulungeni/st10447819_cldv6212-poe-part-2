using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace ABCRetailers.Functions.Functions
{
    public class BlobFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger _logger;

        public BlobFunctions(BlobServiceClient blobServiceClient, ILoggerFactory loggerFactory)
        {
            _blobServiceClient = blobServiceClient;
            _logger = loggerFactory.CreateLogger<BlobFunctions>();
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blob/{containerName}")] HttpRequestData req,
            string containerName)
        {
            _logger.LogInformation($"Simple blob upload to container: {containerName}");

            try
            {
                // Generate a simple unique filename
                var uniqueFileName = $"{Guid.NewGuid()}.jpg";

                _logger.LogInformation($"Uploading file: {uniqueFileName} to container: {containerName}");

                // Get or create container
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                // Upload the entire request body as blob (simple approach)
                var blobClient = containerClient.GetBlobClient(uniqueFileName);
                await blobClient.UploadAsync(req.Body, overwrite: true);

                var blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation($"Successfully uploaded blob: {blobUrl}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { ImageUrl = blobUrl });
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading blob: {ex.Message}\nStack: {ex.StackTrace}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> DeleteBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "blob/{containerName}/{blobName}")] HttpRequestData req,
            string containerName, string blobName)
        {
            _logger.LogInformation($"Deleting blob: {blobName} from container: {containerName}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [Function("ListBlobs")]
        public async Task<HttpResponseData> ListBlobs(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "blob/{containerName}")] HttpRequestData req,
            string containerName)
        {
            _logger.LogInformation($"Listing blobs in container: {containerName}");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobs = new List<string>();
            await foreach (var blob in containerClient.GetBlobsAsync())
            {
                blobs.Add(blob.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(blobs);
            return response;
        }
    }
}
