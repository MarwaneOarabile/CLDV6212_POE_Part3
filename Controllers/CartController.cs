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
    public class CartController(AuthDbContext authContext, IFunctionsApi functionsApi, ILogger<CartController> logger) : Controller
    {
        private readonly AuthDbContext _authContext = authContext;
        private readonly IFunctionsApi _functionsApi = functionsApi;
        private readonly ILogger<CartController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetOrCreateActiveCart(userId);

                var viewModel = await CreateCartViewModel(cart);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading your shopping cart.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProductId) || request.Quantity < 1)
                {
                    return Json(new { success = false, message = "Invalid product or quantity." });
                }

                var userId = GetCurrentUserId();
                var product = await _functionsApi.GetProductAsync(request.ProductId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                if (product.StockAvailable < request.Quantity)
                {
                    return Json(new { success = false, message = $"Insufficient stock. Only {product.StockAvailable} available." });
                }

                var cart = await GetOrCreateActiveCart(userId);
                await AddOrUpdateCartItem(cart, product, request.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Product added to cart!",
                    cartItemCount = cart.CartItems.Sum(ci => ci.Quantity)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to cart");
                return Json(new { success = false, message = "Error adding product to cart." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                if (request.Quantity < 1)
                {
                    return Json(new { success = false, message = "Quantity must be at least 1." });
                }

                var userId = GetCurrentUserId();
                var cartItem = await _authContext.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == request.CartItemId && ci.Cart.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found." });
                }

                // Check stock availability
                var product = await _functionsApi.GetProductAsync(cartItem.ProductId);
                if (product == null || product.StockAvailable < request.Quantity)
                {
                    return Json(new { success = false, message = $"Insufficient stock. Only {product?.StockAvailable ?? 0} available." });
                }

                cartItem.Quantity = request.Quantity;
                cartItem.Cart.UpdatedDate = DateTime.UtcNow;
                await _authContext.SaveChangesAsync();

                var cart = await GetOrCreateActiveCart(userId);
                return Json(new
                {
                    success = true,
                    message = "Quantity updated.",
                    subtotal = cartItem.Subtotal,
                    totalAmount = cart.TotalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for cart item {CartItemId}", request.CartItemId);
                return Json(new { success = false, message = "Error updating quantity." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cartItem = await _authContext.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == request.CartItemId && ci.Cart.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found." });
                }

                _authContext.CartItems.Remove(cartItem);
                cartItem.Cart.UpdatedDate = DateTime.UtcNow;
                await _authContext.SaveChangesAsync();

                var cart = await GetOrCreateActiveCart(userId);
                return Json(new
                {
                    success = true,
                    message = "Item removed from cart.",
                    totalAmount = cart.TotalAmount,
                    cartItemCount = cart.TotalItems
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", request.CartItemId);
                return Json(new { success = false, message = "Error removing item from cart." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetOrCreateActiveCart(userId);

                if (cart.CartItems.Count == 0)
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                var user = await _authContext.Users.FindAsync(userId);
                var cartViewModel = await CreateCartViewModel(cart);

                var checkoutViewModel = new CheckoutViewModel
                {
                    Cart = cartViewModel,
                    ShippingAddress = user?.ShippingAddress ?? string.Empty
                };

                return View(checkoutViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "Error loading checkout page.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetOrCreateActiveCart(userId);

                if (cart.CartItems.Count == 0)
                {
                    ModelState.AddModelError("", "Your cart is empty.");
                    model.Cart = await CreateCartViewModel(cart);
                    return View(model);
                }

                if (ModelState.IsValid)
                {
                    // Update user's shipping address if provided
                    var user = await _authContext.Users.FindAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(model.ShippingAddress))
                    {
                        user.ShippingAddress = model.ShippingAddress;
                    }

                    // ✅ FIX: Get customer record from Azure Table Storage
                    var customer = await _functionsApi.GetCustomerAsync(userId.ToString());
                    if (customer == null)
                    {
                        TempData["Error"] = "Customer profile not found. Please contact support.";
                        model.Cart = await CreateCartViewModel(cart);
                        return View(model);
                    }

                    // Create orders for each cart item
                    foreach (var cartItem in cart.CartItems)
                    {
                        var product = await _functionsApi.GetProductAsync(cartItem.ProductId);
                        if (product != null)
                        {
                            var order = new Order
                            {
                                CustomerId = customer.RowKey, 
                                Username = customer.Username,
                                ProductId = cartItem.ProductId,
                                ProductName = cartItem.ProductName,
                                OrderDate = DateTime.UtcNow,
                                Quantity = cartItem.Quantity,
                                UnitPrice = (double) cartItem.UnitPrice,
                                TotalPrice = (double) cartItem.Subtotal,
                                Status = "Submitted"
                            };

                            // Use Functions API to create order
                            await _functionsApi.CreateOrderAsync(order, customer.RowKey, cartItem.ProductId);
                        }
                    }

                    // Clear the cart after successful checkout
                    _authContext.CartItems.RemoveRange(cart.CartItems);
                    cart.Status = "CheckedOut";
                    cart.UpdatedDate = DateTime.UtcNow;
                    await _authContext.SaveChangesAsync();

                    TempData["Success"] = $"Order placed successfully! {GetPaymentInstructions(model.PaymentMethod)}";
                    return RedirectToAction("Index", "Home");
                }

                // If we got this far, something failed, redisplay form
                model.Cart = await CreateCartViewModel(cart);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {UserId}", GetCurrentUserId());
                TempData["Error"] = "An error occurred during checkout. Please try again.";

                var userId = GetCurrentUserId();
                var cart = await GetOrCreateActiveCart(userId);
                model.Cart = await CreateCartViewModel(cart);
                return View(model);
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

        private async Task<Cart> GetOrCreateActiveCart(int userId)
        {
            var cart = await _authContext.Carts
                .Include(c => c.CartItems)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };
                _authContext.Carts.Add(cart);
                await _authContext.SaveChangesAsync();
            }

            return cart;
        }

        private async Task AddOrUpdateCartItem(Cart cart, Product product, int quantity)
        {
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == product.RowKey);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = product.RowKey,
                    ProductName = product.ProductName,
                    UnitPrice = (decimal)product.Price,
                    Quantity = quantity,
                    AddedDate = DateTime.UtcNow
                };
                _authContext.CartItems.Add(cartItem);
            }

            cart.UpdatedDate = DateTime.UtcNow;
            await _authContext.SaveChangesAsync();
        }

        private async Task<CartViewModel> CreateCartViewModel(Cart cart)
        {
            var viewModel = new CartViewModel
            {
                CartId = cart.CartId,
                ShippingAddress = cart.User?.ShippingAddress ?? string.Empty
            };

            // Get product details from Azure Table Storage for each cart item
            foreach (var item in cart.CartItems)
            {
                var product = await _functionsApi.GetProductAsync(item.ProductId);
                viewModel.Items.Add(new CartItemViewModel
                {
                    CartItemId = item.CartItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    ImageUrl = product?.ImageUrl,
                    StockAvailable = product?.StockAvailable ?? 0
                });
            }

            return viewModel;
        }

        
        public record UpdateQuantityRequest(int CartItemId, int Quantity);
        public record RemoveItemRequest(int CartItemId);

        private static string GetPaymentInstructions(string paymentMethod)
        {
            return paymentMethod == "Online"
                ? "Please upload your proof of payment in the Upload section."
                : "Please have cash ready for payment on delivery/collection.";
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                var cart = await GetOrCreateActiveCart(userId);
                var itemCount = cart.CartItems.Sum(ci => ci.Quantity);

                return Json(new { success = true, cartItemCount = itemCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return Json(new { success = false, cartItemCount = 0 });
            }
        }
    }
}
