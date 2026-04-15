namespace RefactorHeatAlertPostGre.Services.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Broadcasts a message to all active subscribers
        /// </summary>
        Task<int> BroadcastAlertAsync(string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Broadcasts a message with inline keyboard (web app button)
        /// </summary>
        Task<int> BroadcastAlertWithKeyboardAsync(string message, string webAppUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a direct message to a specific subscriber
        /// </summary>
        Task<bool> SendDirectMessageAsync(long chatId, string message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the count of active subscribers
        /// </summary>
        Task<int> GetActiveSubscriberCountAsync(CancellationToken cancellationToken = default);
    }
}