using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Models;

namespace ABCRetailers.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly TableClient _tableClient;
        private readonly ILogger _logger;

        public CustomersFunctions(TableServiceClient tableServiceClient, ILoggerFactory loggerFactory)
        {
            _tableClient = tableServiceClient.GetTableClient("Customers");
            _logger = loggerFactory.CreateLogger<CustomersFunctions>();
        }

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAll(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Customers")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all customers");

            await _tableClient.CreateIfNotExistsAsync();

            var customers = new List<Customer>();
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customers);
            return response;
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "table/Customers/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Getting customer: {partitionKey}/{rowKey}");

            try
            {
                var customer = await _tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customer.Value);
                return response;
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        [Function("AddCustomer")]
        public async Task<HttpResponseData> Add(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table/Customers")] HttpRequestData req)
        {
            _logger.LogInformation("Adding new customer");

            var customer = await req.ReadFromJsonAsync<Customer>();
            if (customer == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.AddEntityAsync(customer);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(customer);
            return response;
        }

        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "table/Customers")] HttpRequestData req)
        {
            _logger.LogInformation("Updating customer");

            var customer = await req.ReadFromJsonAsync<Customer>();
            if (customer == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            await _tableClient.UpsertEntityAsync(customer);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(customer);
            return response;
        }

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> Delete(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "table/Customers/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey, string rowKey)
        {
            _logger.LogInformation($"Deleting customer: {partitionKey}/{rowKey}");

            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}
