using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Models.Enums;
using RefactorHeatAlertPostGre.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace RefactorHeatAlertPostGre.Services
{
    public class TelegramBotService : ITelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISimulationService _simulationService;
        private readonly IGeoService _geoService;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly Dictionary<long, string> _pendingSimulations = new();
        private CancellationTokenSource? _cts;

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public TelegramBotService(
            string botToken,
            IServiceProvider serviceProvider,
            ISimulationService simulationService,
            IGeoService geoService,
            ILogger<TelegramBotService> logger)
        {
            _botClient = new TelegramBotClient(botToken);
            _serviceProvider = serviceProvider;
            _simulationService = simulationService;
            _geoService = geoService;
            _logger = logger;
        }

        public void StartReceiving(CancellationToken cancellationToken = default)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );

            _logger.LogInformation("🤖 Telegram Bot started and listening");
        }

        public async Task StopReceivingAsync()
        {
            if (_cts != null)
            {
                await _cts.CancelAsync();
                _cts.Dispose();
                _cts = null;
            }
            _logger.LogInformation("Telegram Bot stopped");
        }

        public ITelegramBotClient GetClient() => _botClient;

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message?.Location != null)
                {
                    await ProcessLocationPingAsync(botClient, update.Message, cancellationToken);
                    return;
                }

                if (update.Message?.Text == null) return;

                var message = update.Message;
                var chatId = message.Chat.Id;
                var username = message.From?.Username ?? "UnknownUser";
                var text = message.Text.ToLower().Trim();

                await HandleTextCommandAsync(botClient, chatId, username, text, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task HandleTextCommandAsync(
    ITelegramBotClient botClient, 
    long chatId, 
    string username, 
    string text, 
    CancellationToken cancellationToken)
    {
        // Short simulation commands (original style)
        var shortSimCommands = new Dictionary<string, string>
        {
            { "/exdanger", "extreme" },
            { "/danger", "danger" },
            { "/caution", "caution" },
            { "/normal", "normal" },
            { "/cool", "cool" }
        };

        // Long simulation commands (new style)
        var longSimCommands = new Dictionary<string, string>
        {
            { "/simulate extreme", "extreme" },
            { "/simulate danger", "danger" },
            { "/simulate caution", "caution" },
            { "/simulate normal", "normal" },
            { "/simulate cool", "cool" }
        };

        // Check short commands first
        if (shortSimCommands.TryGetValue(text, out var level))
        {
            _pendingSimulations[chatId] = level;
            await RequestLocationForSimulation(botClient, chatId, level, cancellationToken);
            return;
        }

        // Check long commands
        var matchedLong = longSimCommands.FirstOrDefault(c => text.StartsWith(c.Key));
        if (!string.IsNullOrEmpty(matchedLong.Key))
        {
            _pendingSimulations[chatId] = matchedLong.Value;
            await RequestLocationForSimulation(botClient, chatId, matchedLong.Value, cancellationToken);
            return;
        }

        // Regular commands
        switch (text)
        {
            case "/start":
            case "/subscribe":
                await HandleSubscribeAsync(botClient, chatId, username, cancellationToken);
                break;

            case "/unsubscribe":
                await HandleUnsubscribeAsync(botClient, chatId, cancellationToken);
                break;

            case "/status":
                await HandleStatusAsync(botClient, chatId, cancellationToken);
                break;

            case "/help":
                await HandleHelpAsync(botClient, chatId, cancellationToken);
                break;

            default:
                if (_pendingSimulations.ContainsKey(chatId))
                {
                    await botClient.SendMessage(chatId, 
                        "📍 Please send your location using the button below.", 
                        cancellationToken: cancellationToken);
                }
                break;
        }
    }

    private async Task RequestLocationForSimulation(ITelegramBotClient botClient, long chatId, string level, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("📡 Confirm Sensor Location") { RequestLocation = true }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: $"🛠️ **Simulation: {level.ToUpper()}**\n\nTap the button below to send GPS.",
            parseMode: ParseMode.Markdown,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken
        );
    }

        private async Task HandleSubscribeAsync(ITelegramBotClient botClient, long chatId, string username, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var subscriberRepository = scope.ServiceProvider.GetRequiredService<ISubscriberRepository>();

            var subscriber = new Subscriber
            {
                ChatId = chatId,
                Username = username,
                IsSubscribed = true,
                SubscribedAt = DateTime.UtcNow
            };

            await subscriberRepository.SaveAsync(subscriber, cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: "✅ **Subscription Active!**\n\nYou will receive real-time heat alerts for Talisay City.\n\n" +
                      "Commands:\n" +
                      "/status - Check current heat status\n" +
                      "/simulate danger - Simulate an alert (for testing)\n" +
                      "/unsubscribeservice - Stop receiving alerts\n" +
                      "/help - Show all commands",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("New subscriber: {ChatId} ({Username})", chatId, username);
        }

        private async Task HandleUnsubscribeAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var subscriberRepository = scope.ServiceProvider.GetRequiredService<ISubscriberRepository>();

            var success = await subscriberRepository.UnsubscribeAsync(chatId, cancellationToken);

            var message = success
                ? "🔕 **Alerts Muted**\n\nYou will no longer receive heat notifications.\nSend `/subscribeservice` anytime to re-enable them."
                : "❌ You weren't subscribed.";

            await botClient.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleStatusAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var subscriberRepository = scope.ServiceProvider.GetRequiredService<ISubscriberRepository>();

            var activeSensors = await sensorRepository.GetAllActiveAsync(cancellationToken);
            var subscriberCount = await subscriberRepository.GetAllActiveChatIdsAsync(cancellationToken);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("📊 **HEATSYNC STATUS**");
            sb.AppendLine();
            sb.AppendLine($"🚦 Active Sensors: {activeSensors.Count}");
            sb.AppendLine($"👥 Subscribers: {subscriberCount.Count}");
            sb.AppendLine($"🕐 Server Time: {DateTime.Now:hh:mm tt}");
            sb.AppendLine();
            sb.AppendLine("📍 *Active Locations:*");

            foreach (var sensor in activeSensors.Take(10))
            {
                sb.AppendLine($"  • {sensor.DisplayName} ({sensor.Barangay})");
            }

            if (activeSensors.Count > 10)
            {
                sb.AppendLine($"  ... and {activeSensors.Count - 10} more");
            }

            await botClient.SendMessage(
                chatId: chatId,
                text: sb.ToString(),
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleHelpAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            var helpText = 
                "🤖 **HEATSYNC BOT COMMANDS**\n\n" +
                "📋 *General*\n" +
                "/start - Subscribe to alerts\n" +
                "/subscribe - Subscribe to alerts\n" +
                "/unsubscribe - Stop alerts\n" +
                "/status - View system status\n" +
                "/help - Show this help\n\n" +
                "🧪 *Testing (Simulation)*\n" +
                "/exdanger - Simulate Extreme Danger\n" +
                "/danger - Simulate Danger\n" +
                "/caution - Simulate Caution\n" +
                "/normal - Simulate Normal\n" +
                "/cool - Simulate Cool\n\n" +
                "📍 After sending a simulate command, you'll be prompted to share your location.";

            await botClient.SendMessage(
                chatId: chatId,
                text: helpText,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
        }

        private async Task HandleSimulateCommandAsync(ITelegramBotClient botClient, long chatId, string command, CancellationToken cancellationToken)
        {
            var simCommands = new Dictionary<string, string>
            {
                { "/simulate cool", "cool" },
                { "/simulate normal", "normal" },
                { "/simulate caution", "caution" },
                { "/simulate danger", "danger" },
                { "/simulate extreme", "extreme" }
            };

            var matchedCommand = simCommands.FirstOrDefault(c => command.StartsWith(c.Key));

            if (!string.IsNullOrEmpty(matchedCommand.Key))
            {
                _pendingSimulations[chatId] = matchedCommand.Value;

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("📡 Confirm Sensor Location") { RequestLocation = true }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await botClient.SendMessage(
                    chatId: chatId,
                    text: $"🛠️ **Simulation: {matchedCommand.Value.ToUpper()}**\n\nTap the button below to send GPS.",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken
                );
            }
        }

        private async Task ProcessLocationPingAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();

            var chatId = message.Chat.Id;

            if (!_pendingSimulations.TryGetValue(chatId, out var command))
            {
                command = "danger";
            }

            var username = message.From?.Username ?? "UnknownUser";

            // Ensure mobile sensor exists
            await EnsureMobileSensorAsync(sensorRepository, chatId, username, cancellationToken);

            var sensorCode = $"MOBILE_{chatId}";
            var sensor = await sensorRepository.GetByCodeAsync(sensorCode, cancellationToken);

            if (sensor == null)
            {
                await botClient.SendMessage(chatId, "❌ Failed to create mobile sensor.", cancellationToken: cancellationToken);
                return;
            }

            // Determine target heat index
            int targetHeat = command switch
            {
                "extreme" => 85,
                "danger" => 46,
                "caution" => 40,
                "normal" => 31,
                _ => 25
            };

            var lat = message.Location!.Latitude;
            var lng = message.Location.Longitude;
            var barangay = _geoService.GetBarangay(lat, lng);

            // Update sensor location
            sensor.Latitude = (decimal)lat;
            sensor.Longitude = (decimal)lng;
            sensor.Barangay = barangay;
            sensor.IsActive = true;

            await sensorRepository.UpdateAsync(sensor, cancellationToken);

            // Set manual override
            SimulationService.SetManualOverride(sensor.Id, targetHeat, 5);

            // Process and broadcast
            var result = await alertService.ProcessHeatReadingAsync(sensor, targetHeat, cancellationToken);

            await botClient.SendMessage(
                chatId: chatId,
                text: $"📍 Mobile sensor activated in **{barangay}**\n🔥 Heat Index: {targetHeat}°C\n\nAlert has been broadcast to all subscribers.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );

            _pendingSimulations.Remove(chatId);
            _logger.LogInformation("Manual simulation completed for {ChatId} in {Barangay}", chatId, barangay);
        }

        private async Task EnsureMobileSensorAsync(ISensorRepository sensorRepository, long chatId, string username, CancellationToken cancellationToken)
        {
            var sensorCode = $"MOBILE_{chatId}";
            var existing = await sensorRepository.GetByCodeAsync(sensorCode, cancellationToken);

            if (existing == null)
            {
                var newSensor = new Sensor
                {
                    SensorCode = sensorCode,
                    DisplayName = $"{username} (Mobile)",
                    Barangay = "Dynamic GPS",
                    Latitude = 10.2399m,
                    Longitude = 123.8162m,
                    BaselineTemp = 25,
                    EnvironmentType = "Mobile",
                    IsActive = false
                };

                await sensorRepository.CreateAsync(newSensor, cancellationToken);
                _logger.LogInformation("Created mobile sensor for {ChatId}", chatId);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram Bot Error");
            return Task.CompletedTask;
        }
    }
}