using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RefactorHeatAlertPostGre.Infrastructure.BackgroundServices
{
    public class RenderKeepAliveService : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RenderKeepAliveService> _logger;
        private readonly string _pingUrl;
        private readonly TimeSpan _pingInterval = TimeSpan.FromMinutes(10);

        public RenderKeepAliveService(ILogger<RenderKeepAliveService> logger)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            
            // Get from environment or config (default to your Render backend URL)
            _pingUrl = Environment.GetEnvironmentVariable("RENDER_PING_URL") 
                       ?? "https://refactorheatalertpostgreserver.onrender.com/";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait 30 seconds on startup to let app fully initialize
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            _logger.LogInformation("Keep-alive service started, pinging {Url} every {Minutes}min", 
                _pingUrl, _pingInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync(_pingUrl, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("Keep-alive ping successful");
                    }
                    else
                    {
                        _logger.LogWarning("Keep-alive ping returned {StatusCode}", response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Keep-alive ping failed");
                }

                await Task.Delay(_pingInterval, stoppingToken);
            }
        }

        public override void Dispose()
        {
            _httpClient.Dispose();
            base.Dispose();
        }
    }
}