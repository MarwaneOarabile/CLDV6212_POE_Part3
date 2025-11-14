using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers_ST10436124.Controllers
{
    public class ProductController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IFunctionsApi functionsApi, ILogger<ProductController> logger)
        {
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // Everyone can view products
        public async Task<IActionResult> Index()
        {
            var products = await _functionsApi.GetProductsAsync();
            return View(products);
        }

        // Only authenticated users can create products
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // Manual price parsing to fix binding issue
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                _logger.LogInformation("Raw price from form: '{PriceFormValue}'", priceFormValue.ToString());
                if (double.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to parse price: {PriceFormValue}", priceFormValue.ToString());
                }
            }

            _logger.LogInformation("Final product price: {Price}", product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // ✅ FIRST create the product to get a valid RowKey in the database
                    var createdProduct = await _functionsApi.CreateProductAsync(product);

                    // ✅ THEN upload the image using the actual RowKey from the created product
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var imageUrl = await _functionsApi.UploadProductImageAsync(createdProduct.RowKey, imageFile);

                            // ✅ Update the product with the image URL
                            createdProduct.ImageUrl = imageUrl;
                            await _functionsApi.UpdateProductAsync(createdProduct);
                        }
                        catch (Exception imageEx)
                        {
                            _logger.LogWarning(imageEx, "Image upload failed for product {ProductId}, but product was created", createdProduct.RowKey);
                            // Continue without image - product is still created
                        }
                    }

                    TempData["Success"] = $"Product '{createdProduct.ProductName}' created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var product = await _functionsApi.GetProductAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            // Manual price parsing for edit too
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (double.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _functionsApi.UploadProductImageAsync(product.RowKey, imageFile);
                        product.ImageUrl = imageUrl;
                    }

                    // ✅ Update the product (with or without new image)
                    await _functionsApi.UpdateProductAsync(product);

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _functionsApi.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var allProducts = await _functionsApi.GetProductsAsync();
            var searchResults = allProducts.Where(p =>
                p.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SearchResultsCount = searchResults.Count;

            return View("Index", searchResults);
        }
    }
}