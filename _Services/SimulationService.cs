using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Models.Enums;
using RefactorHeatAlertPostGre.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace RefactorHeatAlertPostGre.Services
{
    public class SimulationService : ISimulationService
    {
        private readonly Random _random = new();
        private readonly ILogger<SimulationService> _logger;

        // Track manual override sessions (sensorId -> remaining cycles, fixed heat index)
        private static readonly Dictionary<int, (int RemainingCycles, int FixedHeatIndex)> _manualSessions = new();

        public SimulationService(ILogger<SimulationService> logger)
        {
            _logger = logger;
        }

        public int GenerateReading(Sensor sensor)
        {
            // Check for manual override first
            if (_manualSessions.TryGetValue(sensor.Id, out var session))
            {
                _logger.LogDebug("Manual override active for sensor {SensorCode}: {HeatIndex}°C", 
                    sensor.SensorCode, session.FixedHeatIndex);
                return session.FixedHeatIndex;
            }

            int baseline = sensor.BaselineTemp;
            int roll = _random.Next(1, 101);
            int result;

            if (roll <= 85) // 85% chance: Normal fluctuations
            {
                int normalValue = _random.Next(baseline - 5, baseline + 8);
                result = Math.Clamp(normalValue, 25, 41);
            }
            else // 15% chance: Extreme swings
            {
                int extremeValue = _random.Next(baseline - 20, baseline + 40);
                result = Math.Clamp(extremeValue, 15, 89);
            }

            // Apply environment modifier
            result = ApplyEnvironmentModifier(result, sensor.EnvironmentType);

            return result;
        }

        private int ApplyEnvironmentModifier(int temp, string environmentType)
        {
            return environmentType.ToLower() switch
            {
                "concrete" or "urban" => Math.Min(temp + _random.Next(1, 4), 89),
                "vegetated" or "park" => Math.Max(temp - _random.Next(1, 3), 15),
                "coastal" => temp - _random.Next(0, 2),
                "industrial" => Math.Min(temp + _random.Next(2, 5), 89),
                _ => temp
            };
        }

        public DangerLevel GetDangerLevel(int heatIndex)
        {
            return heatIndex switch
            {
                >= 49 => DangerLevel.ExtremeDanger,
                >= 42 => DangerLevel.Danger,
                >= 38 => DangerLevel.Caution,
                >= 26 => DangerLevel.Normal,
                _ => DangerLevel.Cool
            };
        }

        public AlertResult CreateAlertResult(Sensor sensor, int heatIndex)
        {
            return new AlertResult
            {
                SensorCode = sensor.SensorCode,
                DisplayName = sensor.DisplayName,
                BarangayName = sensor.Barangay,
                RelativeLocation = sensor.DisplayName,
                Latitude = (double)sensor.Latitude,
                Longitude = (double)sensor.Longitude,
                HeatIndex = heatIndex,
                CreatedAt = GetPhilippineTime(),
                DangerLevel = GetDangerLevel(heatIndex).GetDisplayName()
            };
        }

        public async Task<List<AlertResult>> RunSimulationCycleAsync(CancellationToken cancellationToken = default)
        {
            // This would need ISensorRepository - implement after DI is set up
            throw new NotImplementedException("Requires sensor repository injection - implement in background service");
        }

        // Static methods for manual override management
        public static void SetManualOverride(int sensorId, int heatIndex, int cycles = 5)
        {
            _manualSessions[sensorId] = (cycles, heatIndex);
        }

        public static bool TryGetManualSession(int sensorId, out (int RemainingCycles, int FixedHeatIndex) session)
        {
            return _manualSessions.TryGetValue(sensorId, out session);
        }

        public static void DecrementManualSession(int sensorId)
        {
            if (_manualSessions.TryGetValue(sensorId, out var session))
            {
                session.RemainingCycles--;
                if (session.RemainingCycles <= 0)
                {
                    _manualSessions.Remove(sensorId);
                }
                else
                {
                    _manualSessions[sensorId] = session;
                }
            }
        }

        public static void ClearManualSession(int sensorId)
        {
            _manualSessions.Remove(sensorId);
        }

        private DateTime GetPhilippineTime()
        {
            var phZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phZone);
        }
    }
}