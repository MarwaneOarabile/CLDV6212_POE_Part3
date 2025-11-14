using ABCRetailers_ST10436124.Models;

namespace ABCRetailers_ST10436124.Services
{
    public interface IFunctionsApi
    {
        // Customer operations
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerAsync(string id);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task<Customer> UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string id);

        // Product operations
        Task<List<Product>> GetProductsAsync();
        Task<Product?> GetProductAsync(string id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task DeleteProductAsync(string id);
        Task<string> UploadProductImageAsync(string productId, IFormFile imageFile);

        // Order operations
        Task<List<Order>> GetOrdersAsync();
        Task<Order?> GetOrderAsync(string id);
        Task<Order> CreateOrderAsync(Order order, string customerId, string productId);
        Task<bool> UpdateOrderStatusAsync(string orderId, string newStatus);
        Task DeleteOrderAsync(string id);

        // File upload
        Task<string> UploadFileAsync(IFormFile file);
    }
}