using Microsoft.AspNetCore.Mvc;
using RefactorHeatAlertPostGre.Data.Repositories;
using RefactorHeatAlertPostGre.Models.Dto;
using RefactorHeatAlertPostGre.Models.Entities;
using RefactorHeatAlertPostGre.Services.Interfaces;

namespace RefactorHeatAlertPostGre.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensorsController : ControllerBase
    {
        private readonly ISensorRepository _sensorRepository;
        private readonly IGeoService _geoService;
        private readonly ILogger<SensorsController> _logger;

        public SensorsController(
            ISensorRepository sensorRepository,
            IGeoService geoService,
            ILogger<SensorsController> logger)
        {
            _sensorRepository = sensorRepository;
            _geoService = geoService;
            _logger = logger;
        }

        /// <summary>
        /// Get all sensors (optionally include inactive)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SensorDto>>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var sensors = await _sensorRepository.GetAllAsync(includeInactive);
            var dtos = sensors.Select(MapToDto).ToList();
            return Ok(ApiResponse<List<SensorDto>>.Ok(dtos, $"Retrieved {dtos.Count} sensors"));
        }

        /// <summary>
        /// Get a sensor by ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> GetById(int id)
        {
            var sensor = await _sensorRepository.GetByIdAsync(id);
            if (sensor == null)
                return NotFound(ApiResponse<SensorDto>.Fail($"Sensor {id} not found"));

            return Ok(ApiResponse<SensorDto>.Ok(MapToDto(sensor)));
        }

        /// <summary>
        /// Get a sensor by code
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> GetByCode(string code)
        {
            var sensor = await _sensorRepository.GetByCodeAsync(code);
            if (sensor == null)
                return NotFound(ApiResponse<SensorDto>.Fail($"Sensor with code '{code}' not found"));

            return Ok(ApiResponse<SensorDto>.Ok(MapToDto(sensor)));
        }

        /// <summary>
        /// Register a new sensor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SensorDto>>> Create([FromBody] CreateSensorDto dto)
        {
            // Validate coordinates
            if (!_geoService.IsValidCoordinate(dto.Latitude, dto.Longitude))
                return BadRequest(ApiResponse<SensorDto>.Fail("Invalid coordinates"));

            // Check for duplicate code
            if (await _sensorRepository.ExistsByCodeAsync(dto.SensorCode))
                return Conflict(ApiResponse<SensorDto>.Fail($"Sensor code '{dto.SensorCode}' already exists"));

            // Auto-detect barangay if not provided
            if (string.IsNullOrWhiteSpace(dto.Barangay) || dto.Barangay == "string")
                dto.Barangay = _geoService.GetBarangay(dto.Latitude, dto.Longitude);

            var sensor = new Sensor
            {
                SensorCode = dto.SensorCode,
                DisplayName = dto.DisplayName,
                Barangay = dto.Barangay ?? "Unknown",
                Latitude = (decimal)dto.Latitude,
                Longitude = (decimal)dto.Longitude,
                BaselineTemp = dto.BaselineTemp,
                EnvironmentType = dto.EnvironmentType,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _sensorRepository.CreateAsync(sensor);
            _logger.LogInformation("Sensor {Code} registered", created.SensorCode);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, 
                ApiResponse<SensorDto>.Ok(MapToDto(created), "Sensor registered successfully"));
        }

        /// <summary>
        /// Update a sensor (partial update)
        /// </summary>
        [HttpPatch("{id:int}")]
        public async Task<ActionResult<ApiResponse<SensorDto>>> Update(int id, [FromBody] UpdateSensorDto dto)
        {
            var sensor = await _sensorRepository.GetByIdAsync(id);
            if (sensor == null)
                return NotFound(ApiResponse<SensorDto>.Fail($"Sensor {id} not found"));

            // Apply updates
            if (dto.SensorCode != null) sensor.SensorCode = dto.SensorCode;
            if (dto.DisplayName != null) sensor.DisplayName = dto.DisplayName;
            if (dto.Barangay != null) sensor.Barangay = dto.Barangay;
            if (dto.Latitude.HasValue) sensor.Latitude = (decimal)dto.Latitude.Value;
            if (dto.Longitude.HasValue) sensor.Longitude = (decimal)dto.Longitude.Value;
            if (dto.BaselineTemp.HasValue) sensor.BaselineTemp = dto.BaselineTemp.Value;
            if (dto.EnvironmentType != null) sensor.EnvironmentType = dto.EnvironmentType;
            if (dto.IsActive.HasValue) sensor.IsActive = dto.IsActive.Value;

            // Re-detect barangay if coordinates changed and no explicit barangay provided
            if ((dto.Latitude.HasValue || dto.Longitude.HasValue) && string.IsNullOrWhiteSpace(dto.Barangay))
            {
                sensor.Barangay = _geoService.GetBarangay((double)sensor.Latitude, (double)sensor.Longitude);
            }

            sensor.UpdatedAt = DateTime.UtcNow;

            var updated = await _sensorRepository.UpdateAsync(sensor);
            return Ok(ApiResponse<SensorDto>.Ok(MapToDto(updated), "Sensor updated"));
        }

        /// <summary>
        /// Delete a sensor (permanently)
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(int id)
        {
            var exists = await _sensorRepository.ExistsAsync(id);
            if (!exists)
                return NotFound(ApiResponse<bool>.Fail($"Sensor {id} not found"));

            var deleted = await _sensorRepository.DeleteAsync(id);
            return Ok(ApiResponse<bool>.Ok(deleted, deleted ? "Sensor deleted" : "Delete failed"));
        }

        private static SensorDto MapToDto(Sensor sensor) => new()
        {
            Id = sensor.Id,
            SensorCode = sensor.SensorCode,
            DisplayName = sensor.DisplayName,
            Barangay = sensor.Barangay,
            Latitude = (double)sensor.Latitude,
            Longitude = (double)sensor.Longitude,
            BaselineTemp = sensor.BaselineTemp,
            EnvironmentType = sensor.EnvironmentType,
            IsActive = sensor.IsActive
        };
    }
}