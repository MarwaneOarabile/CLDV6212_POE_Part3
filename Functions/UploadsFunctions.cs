using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using ABCRetailers.Functions.Helpers;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;

namespace ABCRetailers.Functions.Functions
{
    public class UploadsFunctions
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ShareServiceClient _shareServiceClient;
        private readonly ILogger<UploadsFunctions> _logger;

        public UploadsFunctions(BlobServiceClient blobServiceClient, ShareServiceClient shareServiceClient, ILogger<UploadsFunctions> logger)
        {
            _blobServiceClient = blobServiceClient;
            _shareServiceClient = shareServiceClient;
            _logger = logger;
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
     [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Upload request received. Content-Type: {ContentType}",
                    req.Headers.GetValues("Content-Type").FirstOrDefault());

                byte[] fileData;
                string fileName;

                // Check if this is multipart form data or raw file
                if (req.Headers.TryGetValues("Content-Type", out var contentTypeValues) &&
                    contentTypeValues.Any(ct => ct.Contains("multipart/form-data")))
                {
                    // Handle multipart form data
                    fileData = await MultipartHelper.ReadFileDataAsync(req);
                    fileName = MultipartHelper.GetFileName(req) ?? $"file_{Guid.NewGuid()}";
                }
                else
                {
                    // Handle raw file upload (what you're actually sending)
                    using var memoryStream = new MemoryStream();
                    await req.Body.CopyToAsync(memoryStream);
                    fileData = memoryStream.ToArray();

                    // Get filename from query string or generate one
                    fileName = req.Query["fileName"];
                    if (string.IsNullOrEmpty(fileName))
                    {
                        // Try to get from content-disposition header
                        if (req.Headers.TryGetValues("Content-Disposition", out var dispositionValues))
                        {
                            var disposition = dispositionValues.FirstOrDefault();
                            if (!string.IsNullOrEmpty(disposition))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(
                                    disposition, @"filename\*?=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    fileName = match.Groups[1].Value.Trim('"');
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(fileName))
                        {
                            // Guess extension from content type
                            var extension = GetFileExtension(req.Headers.GetValues("Content-Type").FirstOrDefault());
                            fileName = $"file_{Guid.NewGuid()}{extension}";
                        }
                    }
                }

                _logger.LogInformation("File data length: {Length}, FileName: {FileName}", fileData.Length, fileName);

                if (fileData.Length == 0)
                {
                    _logger.LogWarning("No file data found in request");
                    return await HttpJson.CreateErrorResponse(req, "No file provided", HttpStatusCode.BadRequest);
                }

                // Upload to Blob Storage
                var blobContainer = _blobServiceClient.GetBlobContainerClient("payment-proofs");
                await blobContainer.CreateIfNotExistsAsync();

                var blobClient = blobContainer.GetBlobClient(fileName);
                using var blobStream = new MemoryStream(fileData);
                await blobClient.UploadAsync(blobStream, overwrite: true);

                _logger.LogInformation("Uploaded file: {FileName} to blob storage", fileName);

                return await HttpJson.CreateJsonResponse(req, new
                {
                    fileName = fileName,
                    blobUrl = blobClient.Uri.ToString(),
                    message = "File uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return await HttpJson.CreateErrorResponse(req, $"Failed to upload file: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        // Helper method to guess file extension from content type
        private static string GetFileExtension(string contentType)
        {
            return contentType?.ToLower() switch
            {
                "application/pdf" => ".pdf",
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "text/plain" => ".txt",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => ".dat"
            };
        }
    }
}