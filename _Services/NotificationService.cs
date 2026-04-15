using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace RefactorHeatAlertPostGre.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ISubscriberRepository _subscriberRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ITelegramBotClient botClient,
            ISubscriberRepository subscriberRepository,
            ILogger<NotificationService> logger)
        {
            _botClient = botClient;
            _subscriberRepository = subscriberRepository;
            _logger = logger;
        }

        public async Task<int> BroadcastAlertAsync(string message, CancellationToken cancellationToken = default)
        {
            var subscribers = await _subscriberRepository.GetAllActiveChatIdsAsync(cancellationToken);
            int successCount = 0;

            foreach (var chatId in subscribers)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: message,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        cancellationToken: cancellationToken
                    );
                    
                    await _subscriberRepository.UpdateLastNotifiedAsync(chatId, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send message to subscriber {ChatId}", chatId);
                    
                    // If user blocked the bot, unsubscribe them
                    if (ex.Message.Contains("bot was blocked") || ex.Message.Contains("user is deactivated"))
                    {
                        await _subscriberRepository.UnsubscribeAsync(chatId, cancellationToken);
                        _logger.LogInformation("Auto-unsubscribed {ChatId} due to blocking", chatId);
                    }
                }
            }

            _logger.LogInformation("Broadcast complete: {SuccessCount}/{TotalCount} delivered", 
                successCount, subscribers.Count);
            
            return successCount;
        }

        public async Task<int> BroadcastAlertWithKeyboardAsync(string message, string webAppUrl, CancellationToken cancellationToken = default)
        {
            var subscribers = await _subscriberRepository.GetAllActiveChatIdsAsync(cancellationToken);
            int successCount = 0;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithWebApp("🌍 OPEN LIVE RADAR", new Telegram.Bot.Types.WebAppInfo { Url = webAppUrl })
                }
            });

            foreach (var chatId in subscribers)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: message,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken
                    );
                    
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send keyboard message to {ChatId}", chatId);
                }
            }

            return successCount;
        }

        public async Task<bool> SendDirectMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: cancellationToken
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send direct message to {ChatId}", chatId);
                return false;
            }
        }

        public async Task<int> GetActiveSubscriberCountAsync(CancellationToken cancellationToken = default)
        {
            var subscribers = await _subscriberRepository.GetAllActiveAsync(cancellationToken);
            return subscribers.Count;
        }
    }
}