

namespace RefactorHeatAlertPostGre.Models.Dto
{
    public class HeatLogDto
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public string SensorCode { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BarangayName { get; set; } = string.Empty;
        public int RecordedTemp { get; set; }
        public int HeatIndex { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime RecordedAt { get; set; }
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string RelativeTime { get; set; } = string.Empty;
    }

    public class HeatHistoryResponse
    {
        public List<HeatLogDto> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }
}