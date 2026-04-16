namespace RefactorHeatAlertPostGre.Models.Dto
{
    public class WokwiReadingDto
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public string? SensorCode { get; set; }  // 👈 New optional field
    }
}