using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace ABCRetailers.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly TableServiceClient _tableServiceClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<ProductsFunctions> _logger;

        public ProductsFunctions(TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient, ILogger<ProductsFunctions> logger)
        {
            _tableServiceClient = tableServiceClient;
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Products");
                var products = new List<ProductDto>();

                await foreach (var entity in tableClient.QueryAsync<ProductEntity>())
                {
                    products.Add(Map.ToDto(entity));
                }

                _logger.LogInformation("Retrieved {Count} products", products.Count);
                return await HttpJson.CreateJsonResponse(req, products); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return await HttpJson.CreateErrorResponse(req, "Failed to retrieve products", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> GetProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Products");
                var product = await tableClient.GetEntityAsync<ProductEntity>("Product", id);

                _logger.LogInformation("Retrieved product with ID: {Id}", id);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(product.Value)); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Product not found", HttpStatusCode.NotFound); // Added await
            }
        }

        [Function("CreateProduct")]
        public async Task<HttpResponseData> CreateProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
        {
            try
            {
                var productDto = await HttpJson.ReadRequestAsync<ProductDto>(req);
                if (productDto == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid product data", HttpStatusCode.BadRequest); // Added await
                }

                var tableClient = _tableServiceClient.GetTableClient("Products");
                var productEntity = Map.ToEntity(productDto);
                await tableClient.AddEntityAsync(productEntity);

                _logger.LogInformation("Created product with ID: {Id}", productEntity.RowKey);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(productEntity), HttpStatusCode.Created); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return await HttpJson.CreateErrorResponse(req, "Failed to create product", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> UpdateProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var productDto = await HttpJson.ReadRequestAsync<ProductDto>(req);
                if (productDto == null)
                {
                    return await HttpJson.CreateErrorResponse(req, "Invalid product data", HttpStatusCode.BadRequest); // Added await
                }

                var tableClient = _tableServiceClient.GetTableClient("Products");
                var existingProduct = await tableClient.GetEntityAsync<ProductEntity>("Product", id);

                var updatedEntity = Map.ToEntity(productDto);
                updatedEntity.RowKey = id;
                updatedEntity.ETag = existingProduct.Value.ETag;

                await tableClient.UpdateEntityAsync(updatedEntity, existingProduct.Value.ETag);

                _logger.LogInformation("Updated product with ID: {Id}", id);
                return await HttpJson.CreateJsonResponse(req, Map.ToDto(updatedEntity)); // Added await
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to update product", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                var tableClient = _tableServiceClient.GetTableClient("Products");
                await tableClient.DeleteEntityAsync("Product", id);

                _logger.LogInformation("Deleted product with ID: {Id}", id);
                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to delete product", HttpStatusCode.InternalServerError); // Added await
            }
        }

        [Function("UploadProductImage")]
        public async Task<HttpResponseData> UploadProductImage(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products/{id}/upload")] HttpRequestData req,
    string id)
        {
            try
            {
                _logger.LogInformation("UploadProductImage request received for product ID: {Id}", id);

                byte[] fileData;
                string fileName;

                // Use the same dual approach we used for UploadFile
                if (req.Headers.TryGetValues("Content-Type", out var contentTypeValues) &&
                    contentTypeValues.Any(ct => ct.Contains("multipart/form-data")))
                {
                    fileData = await MultipartHelper.ReadFileDataAsync(req);
                    fileName = MultipartHelper.GetFileName(req, ".jpg");
                }
                else
                {
                    // Handle raw file upload
                    using var memoryStream = new MemoryStream();
                    await req.Body.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();

                    fileName = req.Query["fileName"];
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = $"product_{id}_{Guid.NewGuid()}.jpg";
                    }
                }

                if (fileData == null || fileData.Length == 0)
                {
                    _logger.LogWarning("No file data found in product image upload for product ID: {Id}", id);
                    return await HttpJson.CreateErrorResponse(req, "No file provided", HttpStatusCode.BadRequest);
                }

                var containerClient = _blobServiceClient.GetBlobContainerClient("product-images");
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(fileName);
                using var stream = new MemoryStream(fileData);
                await blobClient.UploadAsync(stream, overwrite: true);

                var imageUrl = blobClient.Uri.ToString();

                _logger.LogInformation("Uploaded product image for product ID: {Id} to {ImageUrl}", id, imageUrl);

                // Return the image URL - the product will be updated by the web app
                return await HttpJson.CreateJsonResponse(req, new { imageUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image for product ID: {Id}", id);
                return await HttpJson.CreateErrorResponse(req, "Failed to upload image", HttpStatusCode.InternalServerError);
            }
        }
    }
}