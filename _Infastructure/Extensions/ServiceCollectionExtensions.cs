using Microsoft.EntityFrameworkCore;
using Npgsql;
using RefactorHeatAlertPostGre.Data;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Infrastructure.BackgroundServices;
using RefactorHeatAlertPostGre.Services;
using RefactorHeatAlertPostGre.Services.Interfaces;
using Telegram.Bot;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHeatAlertServices(this IServiceCollection services, IConfiguration configuration)
        {
            // --- Connection String Fallback Logic ---
            string connectionString = GetConnectionString(configuration);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                    npgsqlOptions.CommandTimeout(30);
                }));

            // Unit of Work & Repositories (Scoped)
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISensorRepository, SensorRepository>();
            services.AddScoped<IHeatLogRepository, HeatLogRepository>();
            services.AddScoped<ISubscriberRepository, SubscriberRepository>();
            services.AddScoped<IAdminUserRepository, AdminUserRepository>();

            // Core Services
            services.AddSingleton<IGeoService, GeoService>();
            services.AddSingleton<ISimulationService, SimulationService>();
            services.AddScoped<IAlertService, AlertService>();
            services.AddScoped<INotificationService, NotificationService>();

            // Telegram Bot Client (Singleton)
            var botToken = configuration["BotSettings:TelegramToken"] 
                           ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(botToken!));

            // Telegram Bot Service (Singleton)
            services.AddSingleton<ITelegramBotService>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var token = config["BotSettings:TelegramToken"] 
                            ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
                return new TelegramBotService(
                    token!,
                    sp,
                    sp.GetRequiredService<ISimulationService>(),
                    sp.GetRequiredService<IGeoService>(),
                    sp.GetRequiredService<ILogger<TelegramBotService>>()
                );
            });

            // Background Services
            services.AddHostedService<SimulationBackgroundService>();
            services.AddHostedService<RenderKeepAliveService>();

            return services;
        }

        private static string GetConnectionString(IConfiguration configuration)
        {
            // 1. Try Neon environment variable (standard on Render/Neon)
            var neonUrl = Environment.GetEnvironmentVariable("NEON_DATABASE_URL")
                          ?? Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrEmpty(neonUrl) && neonUrl.StartsWith("postgres"))
            {
                Console.WriteLine("🌍 Using Neon PostgreSQL connection from environment.");
                return ConvertPostgresUrlToConnString(neonUrl);
            }

            // 2. Fallback to local appsettings.json
            var localConn = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(localConn))
            {
                Console.WriteLine("💻 Using local PostgreSQL from appsettings.json.");
                return localConn;
            }

            throw new InvalidOperationException("No database connection string found.");
        }

        private static string ConvertPostgresUrlToConnString(string url)
        {
            var uri = new Uri(url);
            var userInfo = uri.UserInfo.Split(':');

            // Default PostgreSQL port if not specified
            int port = uri.Port > 0 ? uri.Port : 5432;

            return $"Host={uri.Host};" +
                $"Port={port};" +
                $"Username={userInfo[0]};" +
                $"Password={userInfo[1]};" +
                $"Database={uri.AbsolutePath.Trim('/')};" +
                $"SSL Mode=Require;" +
                $"Trust Server Certificate=true;" +
                $"Pooling=true;" +
                $"Maximum Pool Size=30;" +
                $"Connection Idle Lifetime=300;";
        }
    }
}