using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;

namespace ABCRetailers.Functions.Helpers
{
    public static class MultipartHelper
    {
        public static async Task<byte[]> ReadFileDataAsync(HttpRequestData request)
        {
            try
            {
                // Check if this is multipart form data
                if (!request.Headers.TryGetValues("Content-Type", out var contentTypeValues) ||
                    !contentTypeValues.Any(ct => ct.Contains("multipart/form-data")))
                {
                    return Array.Empty<byte>();
                }

                var contentType = contentTypeValues.First();
                var boundary = GetBoundary(contentType);

                if (string.IsNullOrEmpty(boundary))
                {
                    return Array.Empty<byte>();
                }

                // Reset the stream position to beginning
                request.Body.Position = 0;

                var reader = new MultipartReader(boundary, request.Body);
                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    var hasContentDisposition = section.Headers.TryGetValue("Content-Disposition", out var dispositionValues);

                    if (hasContentDisposition && dispositionValues.Any(d => d.Contains("form-data")))
                    {
                        var disposition = dispositionValues.First();

                        // Check if this section contains a file
                        if (disposition.Contains("filename="))
                        {
                            using var memoryStream = new MemoryStream();
                            await section.Body.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }

                    section = await reader.ReadNextSectionAsync();
                }

                return Array.Empty<byte>();
            }
            catch (Exception ex)
            {
                // Log the exception if you have access to ILogger
                return Array.Empty<byte>();
            }
        }

        private static string GetBoundary(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return null;

            var elements = contentType.Split(';');
            var boundaryElement = elements.FirstOrDefault(e => e.Trim().StartsWith("boundary="));

            if (boundaryElement == null)
                return null;

            var boundary = boundaryElement.Substring("boundary=".Length).Trim().Trim('"');
            return boundary;
        }

        public static string GetFileName(HttpRequestData request, string defaultExtension = ".dat")
        {
            var fileName = request.Query["fileName"];
            if (!string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            // Try to get filename from multipart sections
            if (request.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                var contentType = contentTypeValues.First();
                var boundary = GetBoundary(contentType);

                if (!string.IsNullOrEmpty(boundary))
                {
                    request.Body.Position = 0;
                    var reader = new MultipartReader(boundary, request.Body);

                    // We'd need to read through sections to find the filename
                    // For simplicity, fall back to generated name
                }
            }

            // Fallback to generated name
            return $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString()[..8]}{defaultExtension}";
        }
    }
}