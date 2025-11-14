using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly ILogger<OrdersFunctions> _logger;

        public OrdersFunctions(TableServiceClient tableServiceClient, QueueServiceClient queueServiceClient, ILogger<OrdersFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _queueServiceClient = queueServiceClient;
            _logger = logger;
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Orders");
                var orders = new List<OrderDto>();

                await foreach (var entity in tableClient.QueryAsync<OrderEntity>())
                {
                    orders.Add(Map.ToDto(entity));
                }

                _logger.LogInformation("Retrieved {Count} orders", orders.Count);
                return await HttpJson.CreateJsonResponse(req, orders); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return await HttpJson.CreateErrorResponse(req, "Failed to retrieve orders", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("GetOrder")]
        public async Task<HttpResponseData> GetOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Orders");
                var order = await tableClient.GetEntityAsync<OrderEntity>("Order", id);

                _logger.LogInformation("Retrieved order with ID: {Id}", id);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(order.Value)); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Order not found", HttpStatusCode.NotFound); // Added await
            }
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> CreateOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var orderCreateDto = await HttpJson.ReadRequestAsync<OrderCreateDto>(req);
                if (orderCreateDto == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid order data", HttpStatusCode.BadRequest); // Added await
                }

                // Get customer and product details
                var customersTable = _tableServiceClient.GetTableClient("Customers");
                var productsTable = _tableServiceClient.GetTableClient("Products");

                var customer = await customersTable.GetEntityAsync<CustomerEntity>("Customer", orderCreateDto.CustomerId);
                var product = await productsTable.GetEntityAsync<ProductEntity>("Product", orderCreateDto.ProductId);

                if (customer == null || product == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid customer or product", HttpStatusCode.BadRequest); // Added await
                }

                // Check stock
                if (product.Value.StockAvailable < orderCreateDto.Quantity)
                {
                    return await HttpJson.CreateErrorResponse(req, $"Insufficient stock. Available: {product.Value.StockAvailable}", HttpStatusCode.BadRequest); // Added await
                }

                // Create order
                var orderEntity = new OrderEntity
                {
                    CustomerId = orderCreateDto.CustomerId,
                    Username = customer.Value.Username,
                    ProductId = orderCreateDto.ProductId,
                    ProductName = product.Value.ProductName,
                    OrderDate = orderCreateDto.OrderDate,
                    Quantity = orderCreateDto.Quantity,
                    UnitPrice = product.Value.Price,
                    TotalPrice = product.Value.Price * orderCreateDto.Quantity,
                    Status = "Submitted"
                };

                var ordersTable = _tableServiceClient.GetTableClient("Orders");
                await ordersTable.AddEntityAsync(orderEntity);

                // Update product stock
                product.Value.StockAvailable -= orderCreateDto.Quantity;
                await productsTable.UpdateEntityAsync(product.Value, product.Value.ETag);

                // Send queue messages
                var orderQueue = _queueServiceClient.GetQueueClient("order-notifications");
                await orderQueue.CreateIfNotExistsAsync();

                var orderMessage = new
                {
                    OrderId = orderEntity.RowKey,
                    CustomerId = orderEntity.CustomerId,
                    CustomerName = $"{customer.Value.Name} {customer.Value.Surname}",
                    ProductName = product.Value.ProductName,
                    Quantity = orderEntity.Quantity,
                    TotalPrice = orderEntity.TotalPrice,
                    OrderDate = orderEntity.OrderDate,
                    Status = orderEntity.Status
                };

                await orderQueue.SendMessageAsync(JsonSerializer.Serialize(orderMessage));

                // Send stock update message
                var stockQueue = _queueServiceClient.GetQueueClient("stock-updates");
                await stockQueue.CreateIfNotExistsAsync();

                var stockMessage = new
                {
                    ProductId = product.Value.RowKey,
                    ProductName = product.Value.ProductName,
                    PreviousStock = product.Value.StockAvailable + orderCreateDto.Quantity,
                    NewStock = product.Value.StockAvailable,
                    UpdatedBy = "Order System",
                    UpdateDate = DateTime.UtcNow
                };

                await stockQueue.SendMessageAsync(JsonSerializer.Serialize(stockMessage));

                _logger.LogInformation("Created order with ID: {Id}", orderEntity.RowKey);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(orderEntity), HttpStatusCode.Created); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return await HttpJson.CreateErrorResponse(req, "Failed to create order", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("UpdateOrderStatus")]
        public async Task<HttpResponseData> UpdateOrderStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "orders/{id}/status")] HttpRequestData req,
            string id)
        {
            try
            {
                var statusUpdate = await HttpJson.ReadRequestAsync<JsonElement>(req);
                if (statusUpdate.ValueKind == JsonValueKind.Null || !statusUpdate.TryGetProperty("newStatus", out JsonElement newStatusProperty))
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid status data", HttpStatusCode.BadRequest); // Added await
                }

                var newStatus = newStatusProperty.GetString();
                var tableClient = _tableServiceClient.GetTableClient("Orders");
                var order = await tableClient.GetEntityAsync<OrderEntity>("Order", id);

                if (order == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Order not found", HttpStatusCode.NotFound); // Added await
                }

                var previousStatus = order.Value.Status;
                order.Value.Status = newStatus ?? "Submitted";
                await tableClient.UpdateEntityAsync(order.Value, order.Value.ETag);

                // Send status update message
                var queueClient = _queueServiceClient.GetQueueClient("order-notifications");
                await queueClient.CreateIfNotExistsAsync();

                var statusMessage = new
                {
                    OrderId = order.Value.RowKey,
                    CustomerId = order.Value.CustomerId,
                    CustomerName = order.Value.Username,
                    ProductName = order.Value.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await queueClient.SendMessageAsync(JsonSerializer.Serialize(statusMessage));

                _logger.LogInformation("Updated order status for ID: {Id} from {Previous} to {New}",
                    id, previousStatus, newStatus);
                return await HttpJson.CreateJsonResponse(req, new { success = true, message = $"Order status updated to {newStatus}" }); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to update order status", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Orders");
                await tableClient.DeleteEntityAsync("Order", id);

                _logger.LogInformation("Deleted order with ID: {Id}", id);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to delete order", HttpStatusCode.InternalServerError); // Added await
            }
        }
    }
}