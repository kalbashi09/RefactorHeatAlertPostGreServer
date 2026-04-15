

namespace RefactorHeatAlertPostGre.Models.Dto
{
    public class SensorDto
    {
        public int Id { get; set; }
        public string SensorCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int BaselineTemp { get; set; }
        public string EnvironmentType { get; set; } = "Unknown";
        public bool IsActive { get; set; }
    }

    public class CreateSensorDto
    {
        public string SensorCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Barangay { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int BaselineTemp { get; set; } = 30;
        public string EnvironmentType { get; set; } = "Unknown";
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSensorDto
    {
        public string? SensorCode { get; set; }
        public string? DisplayName { get; set; }
        public string? Barangay { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? BaselineTemp { get; set; }
        public string? EnvironmentType { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SensorReportRequest
    {
        public int SensorId { get; set; }
        public int Temperature { get; set; }
    }
}