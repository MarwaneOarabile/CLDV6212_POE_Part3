using System.Text;
using System.Text.Json;
using ABCRetailers_ST10436124.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers_ST10436124.Services
{
    public class FunctionsApiClient : IFunctionsApi
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FunctionsApiClient> _logger;
        private readonly string _functionsBaseUrl;

        public FunctionsApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<FunctionsApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _functionsBaseUrl = configuration["Functions:BaseUrl"]
                ?? throw new InvalidOperationException("Functions:BaseUrl not configured");
        }

        private static JsonSerializerOptions JsonOptions => new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Customer methods
        public async Task<List<Customer>> GetCustomersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/customers");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Customer>>(content, JsonOptions) ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers from Functions API");
                throw;
            }
        }

        public async Task<Customer?> GetCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/customers/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer {Id} from Functions API", id);
                throw;
            }
        }

        public async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_functionsBaseUrl}/api/customers", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(responseContent, JsonOptions) ?? customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer via Functions API");
                throw;
            }
        }

        public async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                var json = JsonSerializer.Serialize(customer, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_functionsBaseUrl}/api/customers/{customer.RowKey}", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Customer>(responseContent, JsonOptions) ?? customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {Id} via Functions API", customer.RowKey);
                throw;
            }
        }

        public async Task DeleteCustomerAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionsBaseUrl}/api/customers/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {Id} via Functions API", id);
                throw;
            }
        }

        // Product methods (similar pattern)
        public async Task<List<Product>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/products");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Product>>(content, JsonOptions) ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products from Functions API");
                throw;
            }
        }

        public async Task<Product?> GetProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/products/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {Id} from Functions API", id);
                throw;
            }
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_functionsBaseUrl}/api/products", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(responseContent, JsonOptions) ?? product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product via Functions API");
                throw;
            }
        }

        public async Task<Product> UpdateProductAsync(Product product)
        {
            try
            {
                var json = JsonSerializer.Serialize(product, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{_functionsBaseUrl}/api/products/{product.RowKey}", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Product>(responseContent, JsonOptions) ?? product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {Id} via Functions API", product.RowKey);
                throw;
            }
        }

        public async Task DeleteProductAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionsBaseUrl}/api/products/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {Id} via Functions API", id);
                throw;
            }
        }

        public async Task<string> UploadProductImageAsync(string productId, IFormFile imageFile)
        {
            try
            {
                using var content = new ByteArrayContent(await GetFileBytesAsync(imageFile));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(imageFile.ContentType);

                var response = await _httpClient.PostAsync(
                    $"{_functionsBaseUrl}/api/products/{productId}/upload?fileName={Uri.EscapeDataString(imageFile.FileName)}",
                    content);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return result.GetProperty("imageUrl").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image for product {Id} via Functions API", productId);
                throw;
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            try
            {
                using var formData = new MultipartFormDataContent();
                using var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                formData.Add(fileContent, "file", file.FileName);

                var response = await _httpClient.PostAsync($"{_functionsBaseUrl}/api/upload", formData);

                // Add debugging to see what's happening
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Upload failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                }

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return result.GetProperty("fileName").GetString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file via Functions API");
                throw;
            }
        }

        private static async Task<byte[]> GetFileBytesAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        // Order methods
        public async Task<List<Order>> GetOrdersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/orders");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Order>>(content, JsonOptions) ?? new List<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders from Functions API");
                throw;
            }
        }

        public async Task<Order?> GetOrderAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_functionsBaseUrl}/api/orders/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {Id} from Functions API", id);
                throw;
            }
        }

        public async Task<Order> CreateOrderAsync(Order order, string customerId, string productId)
        {
            try
            {
                var orderData = new
                {
                    CustomerId = customerId,
                    ProductId = productId,
                    order.Quantity,
                    order.OrderDate
                };

                var json = JsonSerializer.Serialize(orderData, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_functionsBaseUrl}/api/orders", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Order>(responseContent, JsonOptions) ?? order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order via Functions API");
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(string orderId, string newStatus)
        {
            try
            {
                var statusData = new { newStatus };
                var json = JsonSerializer.Serialize(statusData, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PatchAsync($"{_functionsBaseUrl}/api/orders/{orderId}/status", content);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId} via Functions API", orderId);
                return false;
            }
        }

        public async Task DeleteOrderAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_functionsBaseUrl}/api/orders/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {Id} via Functions API", id);
                throw;
            }
        }

       
    }
}