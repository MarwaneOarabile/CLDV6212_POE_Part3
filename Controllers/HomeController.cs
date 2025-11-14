using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Models.ViewModels;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;

namespace ABCRetailers_ST10436124.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi functionsApi, IAzureStorageService storageService, ILogger<HomeController> logger)
        {
            _functionsApi = functionsApi;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Redirect authenticated users to their respective dashboards
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("AdminDashboard");
                }
                else if (User.IsInRole("Customer"))
                {
                    return RedirectToAction("CustomerDashboard");
                }
            }

            // For non-authenticated users, show the normal home page
            var products = await _functionsApi.GetProductsAsync();
            var customers = await _functionsApi.GetCustomersAsync();
            var orders = await _functionsApi.GetOrdersAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedProducts = products.Take(5).ToList(),
                ProductCount = products.Count,
                CustomerCount = customers.Count,
                OrderCount = orders.Count
            };
            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _functionsApi.GetCustomersAsync();
                var products = await _functionsApi.GetProductsAsync();
                var orders = await _functionsApi.GetOrdersAsync();

                var model = new AdminDashboardViewModel
                {
                    CustomerCount = customers.Count,
                    ProductCount = products.Count,
                    OrderCount = orders.Count,
                    PendingOrderCount = orders.Count(o => o.Status == "Pending" || o.Status == "Submitted"),
                    RecentOrders = orders
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5)
                        .Select(o => new OrderSummary
                        {
                            OrderId = o.RowKey,
                            CustomerName = o.Username,
                            OrderDate = o.OrderDate,
                            TotalPrice = o.TotalPrice,
                            Status = o.Status
                        })
                        .ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard data");

                // Return empty model if there's an error
                var emptyModel = new AdminDashboardViewModel
                {
                    CustomerCount = 0,
                    ProductCount = 0,
                    OrderCount = 0,
                    PendingOrderCount = 0,
                    RecentOrders = new List<OrderSummary>()
                };

                return View(emptyModel);
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                // Get featured products for customer dashboard
                var products = await _functionsApi.GetProductsAsync();
                var featuredProducts = products.Take(6).ToList(); // Show 6 featured products

                // You can add customer-specific data here later
                ViewData["WelcomeMessage"] = "Welcome to your Dashboard!";

                // Pass featured products to the view
                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer dashboard");
                // Return empty list if there's an error
                return View(new List<Product>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> InitializeStorage()
        {
            try
            {
                // Force re-initialization of storage
                await _storageService.GetAllEntitiesAsync<Customer>();
                TempData["Success"] = "Azure Storage initialized successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to initialize storage: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                return RedirectToAction("CustomerDashboard");
            }
        }
    }
}