using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Services.Interfaces
{
    public interface IAlertService
    {
        /// <summary>
        /// Processes a heat reading and broadcasts alert if danger level is high
        /// </summary>
        Task<AlertResult> ProcessHeatReadingAsync(Sensor sensor, int heatIndex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Broadcasts a heartbeat summary of all alarming sensors
        /// </summary>
        Task BroadcastHeartbeatSummaryAsync(List<AlertResult> readings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves a heat log to the database
        /// </summary>
        Task SaveHeatLogAsync(AlertResult result, int sensorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a heat index warrants an alert
        /// </summary>
        bool ShouldSendAlert(int heatIndex);

        /// <summary>
        /// Formats an alert message for Telegram
        /// </summary>
        string FormatAlertMessage(AlertResult result);
    }
}