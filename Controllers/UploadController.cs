using ABCRetailers_ST10436124.Models;
using ABCRetailers_ST10436124.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers_ST10436124.Controllers
{
    [Authorize]
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _functionsApi;
        private readonly IAzureStorageService _storageService;

        public UploadController(IFunctionsApi functionsApi, IAzureStorageService storageService)
        {
            _functionsApi = functionsApi;
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new FileUploadModel
            {
                Customers = await _storageService.GetAllEntitiesAsync<Customer>(),
                Orders = await _storageService.GetAllEntitiesAsync<Order>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            // Re-populate dropdowns in case validation fails
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Orders = await _storageService.GetAllEntitiesAsync<Order>();

            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        var fileName = await _functionsApi.UploadFileAsync(model.ProofOfPayment);

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";

                        // Return a fresh model with dropdowns
                        return View(new FileUploadModel
                        {
                            Customers = model.Customers,
                            Orders = model.Orders
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            return View(model);
        }
    }
}