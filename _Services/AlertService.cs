using RefactorHeatAlertPostGre.Data;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Models.Enums;
using RefactorHeatAlertPostGre.Services.Interfaces;

namespace RefactorHeatAlertPostGre.Services
{
    public class AlertService : IAlertService
    {
        private readonly IHeatLogRepository _heatLogRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AlertService> _logger;
        private readonly ISimulationService _simulationService;

        public AlertService(
            IHeatLogRepository heatLogRepository,
            ISensorRepository sensorRepository,
            INotificationService notificationService,
            ISimulationService simulationService,
            ILogger<AlertService> logger)
        {
            _heatLogRepository = heatLogRepository;
            _sensorRepository = sensorRepository;
            _notificationService = notificationService;
            _simulationService = simulationService;
            _logger = logger;
        }

        public async Task<AlertResult> ProcessHeatReadingAsync(Sensor sensor, int heatIndex, CancellationToken cancellationToken = default)
        {
            var result = _simulationService.CreateAlertResult(sensor, heatIndex);

            // Always save to database
            await SaveHeatLogAsync(result, sensor.Id, cancellationToken);

            // Only broadcast if it meets alert threshold
            if (ShouldSendAlert(heatIndex))
            {
                var message = FormatAlertMessage(result);
                await _notificationService.BroadcastAlertAsync(message, cancellationToken);
                _logger.LogInformation("Alert broadcasted: {SensorCode} at {HeatIndex}°C", 
                    sensor.SensorCode, heatIndex);
            }

            return result;
        }

        public async Task BroadcastHeartbeatSummaryAsync(List<AlertResult> readings, CancellationToken cancellationToken = default)
        {
            var alarmingSpots = readings
                .Where(r => ShouldSendAlert(r.HeatIndex))
                .OrderByDescending(r => r.HeatIndex)
                .ToList();

            if (!alarmingSpots.Any())
            {
                _logger.LogDebug("No alarming spots in this cycle");
                return;
            }

            var message = FormatHeartbeatMessage(alarmingSpots);

            // ✅ Add the web app URL for the radar button
            string webAppUrl = "https://heatsync-zs03.onrender.com/mapUI.html";

            // ✅ Use the keyboard broadcast method
            await _notificationService.BroadcastAlertWithKeyboardAsync(message, webAppUrl, cancellationToken);

            _logger.LogInformation("Heartbeat broadcasted with {Count} alarming locations", alarmingSpots.Count);
        }

        public async Task SaveHeatLogAsync(AlertResult result, int sensorId, CancellationToken cancellationToken = default)
        {
            var heatLog = new HeatLog
            {
                SensorId = sensorId,
                RecordedTemp = result.HeatIndex,
                HeatIndex = result.HeatIndex,
                RecordedAt = DateTime.UtcNow
            };

            await _heatLogRepository.CreateAsync(heatLog, cancellationToken);

            // Periodic cleanup - keep only latest 300 logs
            if (await _heatLogRepository.GetCountAsync(cancellationToken) > 350)
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);
                var deleted = await _heatLogRepository.DeleteOlderThanAsync(cutoff, cancellationToken);
                _logger.LogDebug("Cleaned up {Count} old heat logs", deleted);
            }
        }

        public bool ShouldSendAlert(int heatIndex)
        {
            return heatIndex >= 38; // Caution level and above
        }

        public string FormatAlertMessage(AlertResult result)
        {
            var level = _simulationService.GetDangerLevel(result.HeatIndex);
            var emoji = level.GetEmoji();

            return $"{emoji} *HEAT ALERT: {level.GetDisplayName()}*\n\n" +
                   $"📍 Location: {result.RelativeLocation} ({result.BarangayName})\n" +
                   $"🔥 Heat Index: {result.HeatIndex}°C\n" +
                   $"⏰ Time: {result.CreatedAt:hh:mm tt}";
        }

        private string FormatHeartbeatMessage(List<AlertResult> alarmingSpots)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("🌡️ ***HEATSYNC: HIGH HEAT REPORT***");
            sb.AppendLine($"⏰ *Scanned at: {DateTime.Now:hh:mm tt}*");
            sb.AppendLine("-----------------------------------");

            var topSpot = alarmingSpots.First();
            sb.AppendLine($"🔝 **HIGHEST:** {topSpot.HeatIndex}°C in {topSpot.BarangayName}");
            sb.AppendLine();

            foreach (var spot in alarmingSpots)
            {
                var level = _simulationService.GetDangerLevel(spot.HeatIndex);
                var emoji = level.GetEmoji();
                
                sb.AppendLine($"{emoji} *{spot.HeatIndex}°C* - {level.GetDisplayName()}");
                sb.AppendLine($"📍 {spot.DisplayName} ({spot.BarangayName})");
                sb.AppendLine();
            }

            sb.AppendLine("📍 *Tap the button below for the live interactive radar.*");
            
            return sb.ToString();
        }
    }
}