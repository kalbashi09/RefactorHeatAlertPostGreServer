using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RefactorHeatAlertPostGre.Infrastructure.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly string _apiKey;
        private readonly string _apiKeyHeader = "X-API-KEY";

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _apiKey = configuration["ApiSettings:ApiKey"] 
                      ?? Environment.GetEnvironmentVariable("API_KEY") 
                      ?? string.Empty;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request requires API key validation
            if (RequiresApiKey(context))
            {
                if (!context.Request.Headers.TryGetValue(_apiKeyHeader, out var extractedKey))
                {
                    _logger.LogWarning("API key missing for {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "API key required" });
                    return;
                }

                if (extractedKey != _apiKey)
                {
                    _logger.LogWarning("Invalid API key for {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid API key" });
                    return;
                }
            }

            await _next(context);
        }

        private bool RequiresApiKey(HttpContext context)
        {
            var path = context.Request.Path;
            var method = context.Request.Method;

            // Protect write operations on sensors
            if (path.StartsWithSegments("/api/sensors"))
            {
                return method == HttpMethods.Post ||
                       method == HttpMethods.Patch ||
                       method == HttpMethods.Delete;
            }

            // Protect the manual heat report endpoint
            if (path.StartsWithSegments("/api/alerts/report"))
            {
                return method == HttpMethods.Post;
            }

            // Protect subscriber management (optional)
            if (path.StartsWithSegments("/api/subscribers"))
            {
                return method == HttpMethods.Post || method == HttpMethods.Delete;
            }

            return false;
        }
    }
}