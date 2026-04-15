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

        // Paths that require API key (you can also use attributes)
        private readonly string[] _protectedPaths = new[]
        {
            "/api/alerts/report",
            "/api/sensors/patch"  // partial example
        };

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
            // Check if the request path requires API key
            if (RequiresApiKey(context.Request.Path))
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

        private bool RequiresApiKey(PathString path)
        {
            // Simple check: any POST/PATCH to protected routes
            // You can customize this logic
            return path.StartsWithSegments("/api/alerts/report") ||
                   path.StartsWithSegments("/api/sensors") && 
                   (path.Value?.Contains("patch", StringComparison.OrdinalIgnoreCase) ?? false);
        }
    }
}