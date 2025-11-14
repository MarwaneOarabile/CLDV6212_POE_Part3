using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Helpers;
using Azure.Data.Tables;

namespace ABCRetailers.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly ILogger<CustomersFunctions> _logger;

        public CustomersFunctions(TableServiceClient tableServiceClient, ILogger<CustomersFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _logger = logger;
        }

        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers")] HttpRequestData req)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var customers = new List<CustomerDto>();

                await foreach (var entity in tableClient.QueryAsync<CustomerEntity>())
                {
                    customers.Add(Map.ToDto(entity));
                }

                _logger.LogInformation("Retrieved {Count} customers", customers.Count);
                return await HttpJson.CreateJsonResponse(req, customers); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return await HttpJson.CreateErrorResponse(req, "Failed to retrieve customers", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var customer = await tableClient.GetEntityAsync<CustomerEntity>("Customer", id);

                _logger.LogInformation("Retrieved customer with ID: {Id}", id);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(customer.Value)); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Customer not found", HttpStatusCode.NotFound); // Added await
            }
        }

        [Function("CreateCustomer")]
        public async Task<HttpResponseData> CreateCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customers")] HttpRequestData req)
        {
            try
            {
                var customerDto = await HttpJson.ReadRequestAsync<CustomerDto>(req);
                if (customerDto == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid customer data", HttpStatusCode.BadRequest); // Added await
                }

                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var customerEntity = Map.ToEntity(customerDto);
                await tableClient.AddEntityAsync(customerEntity);

                _logger.LogInformation("Created customer with ID: {Id}", customerEntity.RowKey);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(customerEntity), HttpStatusCode.Created); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return await HttpJson.CreateErrorResponse(req, "Failed to create customer", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var customerDto = await HttpJson.ReadRequestAsync<CustomerDto>(req);
                if (customerDto == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid customer data", HttpStatusCode.BadRequest); // Added await
                }

                var tableClient = _tableServiceClient.GetTableClient("Customers");
                var existingCustomer = await tableClient.GetEntityAsync<CustomerEntity>("Customer", id);

                var updatedEntity = Map.ToEntity(customerDto);
                updatedEntity.RowKey = id;
                updatedEntity.ETag = existingCustomer.Value.ETag;

                await tableClient.UpdateEntityAsync(updatedEntity, existingCustomer.Value.ETag);

                _logger.LogInformation("Updated customer with ID: {Id}", id);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(updatedEntity)); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to update customer", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Customers");
                await tableClient.DeleteEntityAsync("Customer", id);

                _logger.LogInformation("Deleted customer with ID: {Id}", id);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to delete customer", HttpStatusCode.InternalServerError); // Added await
            }
        }
    }
}