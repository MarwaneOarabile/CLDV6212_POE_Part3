using ABCRetailers_ST10436124.Data;
using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Models.ViewModels;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ABCRetailers_ST10436124.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerOrderController : Controller
    {
        private readonly AuthDbContext _authContext;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<CustomerOrderController> _logger;

        public CustomerOrderController(AuthDbContext authContext, IFunctionsApi functionsApi, ILogger<CustomerOrderController> logger)
        {
            _authContext = authContext;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authContext.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get customer record from Azure Table Storage using the UserId as RowKey
                var customer = await _functionsApi.GetCustomerAsync(userId.ToString());
                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found. Please contact support.";
                    return RedirectToAction("Index", "Home");
                }

                var products = await _functionsApi.GetProductsAsync();
                var viewModel = new CustomerOrderViewModel
                {
                    CustomerId = customer.RowKey,
                    CustomerName = $"{customer.Name} {customer.Surname}",
                    Products = products
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer order form");
                TempData["Error"] = "Error loading order form.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerOrderViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authContext.Users.FindAsync(userId);

                if (user == null)
                {
                    TempData["Error"] = "User not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Get customer record
                var customer = await _functionsApi.GetCustomerAsync(userId.ToString());
                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found.";
                    return RedirectToAction("Create");
                }

                if (ModelState.IsValid)
                {
                    // Get product details
                    var product = await _functionsApi.GetProductAsync(model.ProductId);
                    if (product == null)
                    {
                        ModelState.AddModelError("ProductId", "Invalid product selected.");
                        model.Products = await _functionsApi.GetProductsAsync();
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        model.Products = await _functionsApi.GetProductsAsync();
                        return View(model);
                    }

                    // Create order - automatically use the logged-in customer
                    var order = new Order
                    {
                        PartitionKey = "Orders",
                        RowKey = Guid.NewGuid().ToString(),
                        CustomerId = customer.RowKey, // Use the customer's RowKey from Azure Table
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.UtcNow,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _functionsApi.CreateOrderAsync(order, customer.RowKey, model.ProductId);

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction("MyOrders");
                }

                // If we got here, something failed
                model.Products = await _functionsApi.GetProductsAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer order");
                TempData["Error"] = "Error creating order. Please try again.";
                return RedirectToAction("Create");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            try
            {
                var userId = GetCurrentUserId();
                var customer = await _functionsApi.GetCustomerAsync(userId.ToString());

                if (customer == null)
                {
                    TempData["Error"] = "Customer profile not found.";
                    return View(new List<Order>());
                }

                var allOrders = await _functionsApi.GetOrdersAsync();
                var myOrders = allOrders
                    .Where(o => o.CustomerId == customer.RowKey)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();

                return View(myOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer orders");
                TempData["Error"] = "Error loading your orders.";
                return View(new List<Order>());
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
    }
}