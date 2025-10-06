using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Models;

namespace ABCRetailers.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        public ProductsFunctions(TableServiceClient tableServiceClient, ILoggerFactory loggerFactory)
        {
            _tableClient = tableServiceClient.GetTableClient("Products");
            _logger = loggerFactory.CreateLogger<ProductsFunctions>();
        }

        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Products")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all products");

            await _tableClient.CreateIfNotExistsAsync();

            var products = new List<Product>();
            await foreach (var product in _tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(products);
            return response;
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Products/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Getting product: {partitionKey}/{rowKey}");

            try
            {
                var product = await _tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(product.Value);
                return response;
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("AddProduct")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table/Products")] HttpRequestData req)
        {
            _logger.LogInformation("Adding new product");

            var product = await req.ReadFromJsonAsync<Product>();
            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.AddEntityAsync(product);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "table/Products")] HttpRequestData req)
        {
            _logger.LogInformation("Updating product");

            var product = await req.ReadFromJsonAsync<Product>();
            if (product == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.UpsertEntityAsync(product);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(product);
            return response;
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "table/Products/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Deleting product: {partitionKey}/{rowKey}");

            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
