using Microsoft.EntityFrameworkCore;
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
                options.UseSqlite(connectionString));

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
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection string not found in appsettings.json.");
            }
            return connectionString;
        }

    }
}