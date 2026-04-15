using Telegram.Bot;

namespace RefactorHeatAlertPostGre.Services.Interfaces
{
    public interface ITelegramBotService
    {
        /// <summary>
        /// Starts the bot and begins receiving updates
        /// </summary>
        void StartReceiving(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the bot
        /// </summary>
        Task StopReceivingAsync();

        /// <summary>
        /// Gets the bot client instance
        /// </summary>
        ITelegramBotClient GetClient();

        /// <summary>
        /// Checks if the bot is running
        /// </summary>
        bool IsRunning { get; }
    }
}