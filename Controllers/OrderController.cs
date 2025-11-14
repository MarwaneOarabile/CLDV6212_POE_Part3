using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Models.ViewModels;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCRetailers_ST10436124.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IFunctionsApi functionsApi, IAzureStorageService storageService, ILogger<OrderController> logger)
        {
            _functionsApi = functionsApi;
            _storageService = storageService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _functionsApi.GetOrdersAsync();
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details for validation
                    var customer = await _storageService.GetEntityAsync<Customer>("Customers", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Products", model.ProductId);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order object
                    var order = new Order
                    {
                        PartitionKey = "Orders",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    // Use Functions API to create order
                    await _functionsApi.CreateOrderAsync(order, model.CustomerId, model.ProductId);

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating order");
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var order = await _functionsApi.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var order = await _functionsApi.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // For now, we'll update order through storage service since Functions API doesn't have full update
                    // In a real scenario, we'd add an UpdateOrder function
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    if (originalOrder == null)
                    {
                        return NotFound();
                    }

                    originalOrder.TotalPrice = order.TotalPrice;
                    originalOrder.UnitPrice = order.UnitPrice;
                    originalOrder.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    originalOrder.Quantity = order.Quantity;
                    originalOrder.Status = order.Status;

                    await _storageService.UpdateEntityAsync(originalOrder);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _functionsApi.DeleteOrderAsync(id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var success = await _functionsApi.UpdateOrderStatusAsync(id, newStatus);
                if (success)
                {
                    return Json(new { success = true, message = $"Order status updated to {newStatus}" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update order status" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}