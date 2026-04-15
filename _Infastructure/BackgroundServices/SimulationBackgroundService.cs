using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Services;
using RefactorHeatAlertPostGre.Services.Interfaces;

namespace RefactorHeatAlertPostGre.Infrastructure.BackgroundServices
{
    public class SimulationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SimulationBackgroundService> _logger;
        private readonly TimeSpan _cycleInterval = TimeSpan.FromSeconds(30);

        public SimulationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SimulationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔥 Heat Simulation Engine starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunSimulationCycleAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Simulation cycle failed");
                }

                await Task.Delay(_cycleInterval, stoppingToken);
            }

            _logger.LogInformation("Simulation Engine stopped");
        }

        private async Task RunSimulationCycleAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var heatLogRepository = scope.ServiceProvider.GetRequiredService<IHeatLogRepository>();
            var alertService = scope.ServiceProvider.GetRequiredService<IAlertService>();
            var simulationService = scope.ServiceProvider.GetRequiredService<ISimulationService>();

            var sensors = await sensorRepository.GetAllActiveAsync(cancellationToken);
            if (sensors.Count == 0)
            {
                _logger.LogDebug("No active sensors to simulate");
                return;
            }

            var batchResults = new List<AlertResult>();

            foreach (var sensor in sensors)
            {
                // Check for manual override session
                if (SimulationService.TryGetManualSession(sensor.Id, out var session))
                {
                    var heatIndex = session.FixedHeatIndex;
                    var result = simulationService.CreateAlertResult(sensor, heatIndex);
                    
                    await heatLogRepository.CreateAsync(new HeatLog
                    {
                        SensorId = sensor.Id,
                        RecordedTemp = heatIndex,
                        HeatIndex = heatIndex,
                        RecordedAt = DateTime.UtcNow
                    }, cancellationToken);

                    batchResults.Add(result);

                    SimulationService.DecrementManualSession(sensor.Id);
                    if (session.RemainingCycles <= 1)
                    {
                        _logger.LogInformation("Manual session expired for sensor {Code}", sensor.SensorCode);
                    }
                }
                else
                {
                    // Normal simulation
                    var heatIndex = simulationService.GenerateReading(sensor);
                    var result = await alertService.ProcessHeatReadingAsync(sensor, heatIndex, cancellationToken);
                    batchResults.Add(result);
                }

                _logger.LogDebug("[{Sensor}] {HeatIndex}°C in {Barangay}", 
                    sensor.DisplayName, batchResults.Last().HeatIndex, sensor.Barangay);
            }

            // Broadcast heartbeat summary for all alarming locations
            await alertService.BroadcastHeartbeatSummaryAsync(batchResults, cancellationToken);
        }
    }
}