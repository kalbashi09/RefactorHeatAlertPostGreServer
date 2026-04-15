using Microsoft.AspNetCore.Mvc;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Dto;
using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Services.Interfaces;

namespace RefactorHeatAlertPostGre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        private readonly IHeatLogRepository _heatLogRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly IAlertService _alertService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(
            IHeatLogRepository heatLogRepository,
            ISensorRepository sensorRepository,
            IAlertService alertService,
            IConfiguration configuration,
            ILogger<AlertsController> logger)
        {
            _heatLogRepository = heatLogRepository;
            _sensorRepository = sensorRepository;
            _alertService = alertService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Get current/latest alert (public)
        /// </summary>
        [HttpGet("current")]
        public async Task<ActionResult<ApiResponse<AlertResult>>> GetCurrentAlert()
        {
            var latest = await _heatLogRepository.GetLatestAsync();
            if (latest == null)
                return NotFound(ApiResponse<AlertResult>.Fail("No heat data available yet"));

            var sensor = await _sensorRepository.GetByIdAsync(latest.SensorId);
            var result = new AlertResult
            {
                SensorCode = sensor?.SensorCode ?? "UNKNOWN",
                DisplayName = sensor?.DisplayName ?? "Unknown Sensor",
                BarangayName = sensor?.Barangay ?? "Unknown",
                RelativeLocation = sensor?.DisplayName ?? "Unknown",
                Latitude = sensor != null ? (double)sensor.Latitude : 0,
                Longitude = sensor != null ? (double)sensor.Longitude : 0,
                HeatIndex = latest.HeatIndex,
                CreatedAt = latest.RecordedAt,
                DangerLevel = _alertService.ShouldSendAlert(latest.HeatIndex) ? "ALERT" : "NORMAL"
            };

            return Ok(ApiResponse<AlertResult>.Ok(result));
        }

        /// <summary>
        /// Get heat history (public, supports pagination)
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<HeatHistoryResponse>>> GetHistory(
            [FromQuery] int limit = 300, 
            [FromQuery] int offset = 0)
        {
            var logs = await _heatLogRepository.GetHistoryAsync(limit, offset);
            var totalCount = await _heatLogRepository.GetCountAsync();

            var dtos = new List<HeatLogDto>();
            foreach (var log in logs)
            {
                var sensor = await _sensorRepository.GetByIdAsync(log.SensorId);
                dtos.Add(new HeatLogDto
                {
                    Id = log.Id,
                    SensorId = log.SensorId,
                    SensorCode = sensor?.SensorCode ?? "UNKNOWN",
                    DisplayName = sensor?.DisplayName ?? "Unknown",
                    BarangayName = sensor?.Barangay ?? "Unknown",
                    RecordedTemp = log.RecordedTemp,
                    HeatIndex = log.HeatIndex,
                    Latitude = sensor != null ? (double)sensor.Latitude : 0,
                    Longitude = sensor != null ? (double)sensor.Longitude : 0,
                    RecordedAt = log.RecordedAt,
                    Date = log.RecordedAt.ToString("MMM dd, yyyy"),
                    Time = log.RecordedAt.ToString("hh:mm tt"),
                    RelativeTime = GetRelativeTime(log.RecordedAt)
                });
            }

            var response = new HeatHistoryResponse
            {
                Logs = dtos,
                TotalCount = totalCount,
                Limit = limit,
                Offset = offset
            };

            return Ok(ApiResponse<HeatHistoryResponse>.Ok(response));
        }

        /// <summary>
        /// Submit a heat report from a sensor (requires API key)
        /// </summary>
        [HttpPost("report")]
        public async Task<ActionResult<ApiResponse<object>>> ReportHeat(
            [FromBody] SensorReportRequest request,
            [FromHeader(Name = "X-API-KEY")] string? apiKey)
        {
            // Validate API key
            var expectedKey = _configuration["ApiSettings:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey != expectedKey)
                return Unauthorized(ApiResponse<object>.Fail("Invalid or missing API key"));

            var sensor = await _sensorRepository.GetByIdAsync(request.SensorId);
            if (sensor == null)
                return BadRequest(ApiResponse<object>.Fail($"Sensor {request.SensorId} not found"));

            if (!sensor.IsActive)
                return BadRequest(ApiResponse<object>.Fail("Sensor is inactive"));

            var result = await _alertService.ProcessHeatReadingAsync(sensor, request.Temperature);

            return Ok(ApiResponse<object>.Ok(new { 
                message = "Report processed", 
                sensor = sensor.DisplayName,
                heatIndex = request.Temperature 
            }));
        }

        private static string GetRelativeTime(DateTime time)
        {
            var delta = DateTime.UtcNow - time;
            if (delta.TotalMinutes < 1) return "Just now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            return time.ToString("MMM dd");
        }

        // In AlertsController.cs

        [HttpPost("wokwi-reading")]
        public async Task<IActionResult> ReceiveWokwiReading([FromBody] WokwiReadingDto reading)
        {
            if (reading == null)
                return BadRequest("Invalid data received.");

            // Log the received data (great for debugging)
            _logger.LogInformation($"Received from Wokwi - Temp: {reading.Temperature}°C, Hum: {reading.Humidity}%");

            // --- HERE IS THE MAGIC ---
            // We need to connect this reading to a sensor in your system.
            // Option A: Create a new sensor or find an existing one.
            var sensor = await _sensorRepository.GetByCodeAsync("WOKWI-VIRTUAL-01");
            if (sensor == null)
            {
                // You could create a placeholder sensor the first time data arrives.
                sensor = new Sensor
                {
                    SensorCode = "WOKWI-VIRTUAL-01",
                    DisplayName = "Talisay City College (Wokwi)",
                    Barangay = "Poblacion",
                    Latitude = 10.2429329M, Longitude = 123.848309M, // Wokwi doesn't provide GPS, so use placeholders
                    IsActive = true,
                    IsExternal = true
                };
                sensor = await _sensorRepository.CreateAsync(sensor);
            }

            // Now, use your existing IAlertService to process this reading!
            // The heat index calculation will automatically use the temperature.
            var alertResult = await _alertService.ProcessHeatReadingAsync(sensor, (int)reading.Temperature);
            
            // Optionally, you could also broadcast humidity via Telegram or just log it.
            // ...

            // Return a success response to the Wokwi ESP32
            return Ok(new { message = "Sensor reading processed successfully." });
        }
    }


}