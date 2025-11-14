using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class QueueProcessorFunctions
    {
        private readonly ILogger<QueueProcessorFunctions> _logger;

        public QueueProcessorFunctions(ILogger<QueueProcessorFunctions> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrderNotifications")]
        public async Task ProcessOrderNotifications(
            [QueueTrigger("order-notifications", Connection = "AzureStorageConnection")] string queueMessage)
        {
            try
            {
                var message = JsonSerializer.Deserialize<JsonElement>(queueMessage);
                _logger.LogInformation("Processing order notification: {Message}", queueMessage);

                // Here you could send emails, update other systems, etc.
                // For now, we'll just log the message
                if (message.TryGetProperty("OrderId", out var orderId))
                {
                    _logger.LogInformation("Successfully processed order notification for Order ID: {OrderId}",
                        orderId.GetString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order notification: {Message}", queueMessage);
                throw;
            }
        }

        [Function("ProcessStockUpdates")]
        public async Task ProcessStockUpdates(
            [QueueTrigger("stock-updates", Connection = "AzureStorageConnection")] string queueMessage)
        {
            try
            {
                var message = JsonSerializer.Deserialize<JsonElement>(queueMessage);
                _logger.LogInformation("Processing stock update: {Message}", queueMessage);

                // Process stock updates - could update inventory systems, send alerts, etc.
                if (message.TryGetProperty("ProductId", out var productId))
                {
                    _logger.LogInformation("Successfully processed stock update for Product ID: {ProductId}",
                        productId.GetString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock update: {Message}", queueMessage);
                throw;
            }
        }
    }
}