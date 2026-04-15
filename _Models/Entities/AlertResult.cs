

namespace RefactorHeatAlertPostGre.Models.Entities
{
    /// <summary>
    /// Domain model for alert data - not stored directly in DB.
    /// Used for Telegram notifications and API responses.
    /// </summary>
    public class AlertResult
    {
        public string SensorCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BarangayName { get; set; } = string.Empty;
        public string RelativeLocation { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int HeatIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DangerLevel { get; set; } = string.Empty;
    }
}