

namespace RefactorHeatAlertPostGre.Models.Entities
{
    public class Sensor
    {
        public int Id { get; set; }
        public string SensorCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public int BaselineTemp { get; set; }
        public string EnvironmentType { get; set; } = "Unknown";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<HeatLog> HeatLogs { get; set; } = new List<HeatLog>();

        public bool IsExternal { get; set; } = false; // New property
    }
}