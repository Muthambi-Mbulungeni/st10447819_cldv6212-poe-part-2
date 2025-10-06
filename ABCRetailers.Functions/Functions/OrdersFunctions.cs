using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Models;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersFunctions
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        public OrdersFunctions(TableServiceClient tableServiceClient, ILoggerFactory loggerFactory)
        {
            _tableClient = tableServiceClient.GetTableClient("Orders");
            _logger = loggerFactory.CreateLogger<OrdersFunctions>();
        }

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Orders")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all orders");

            await _tableClient.CreateIfNotExistsAsync();

            var orders = new List<Order>();
            await foreach (var order in _tableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders);
            return response;
        }

        [Function("GetOrder")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Orders/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Getting order: {partitionKey}/{rowKey}");

            try
            {
                var order = await _tableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(order.Value);
                return response;
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("AddOrder")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table/Orders")] HttpRequestData req)
        {
            _logger.LogInformation("Adding new order");

            var order = await req.ReadFromJsonAsync<Order>();
            if (order == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.AddEntityAsync(order);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(order);
            return response;
        }

        [Function("UpdateOrder")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "table/Orders")] HttpRequestData req)
        {
            _logger.LogInformation("Updating order");

            var order = await req.ReadFromJsonAsync<Order>();
            if (order == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.UpsertEntityAsync(order);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(order);
            return response;
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "table/Orders/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Deleting order: {partitionKey}/{rowKey}");

            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
