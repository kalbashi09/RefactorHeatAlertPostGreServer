

namespace RefactorHeatAlertPostGre.Models.Entities
{
    public class HeatLog
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public int RecordedTemp { get; set; }
        public int HeatIndex { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Sensor Sensor { get; set; } = null!;
    }
}