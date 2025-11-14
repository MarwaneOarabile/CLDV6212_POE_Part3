using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABCRetailers.Functions.Helpers
{
    public static class HttpJson
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public static async Task<T?> ReadRequestAsync<T>(HttpRequestData req)
        {
            using var reader = new StreamReader(req.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(content, _jsonOptions);
        }

        public static async Task<HttpResponseData> CreateJsonResponse<T>(
            HttpRequestData req,
            T data,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(statusCode);

            // Set content type header safely
            if (!response.Headers.Contains("Content-Type"))
            {
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            }

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await response.WriteStringAsync(json); // Use async method

            return response;
        }

        public static async Task<HttpResponseData> CreateErrorResponse(
            HttpRequestData req,
            string message,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        {
            var response = req.CreateResponse(statusCode);

            // Set content type header safely
            if (!response.Headers.Contains("Content-Type"))
            {
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            }

            var error = new { error = message };
            var json = JsonSerializer.Serialize(error, _jsonOptions);
            await response.WriteStringAsync(json); // Use async method

            return response;
        }
    }
}