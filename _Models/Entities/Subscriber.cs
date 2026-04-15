
namespace RefactorHeatAlertPostGre.Models.Entities
{
    public class Subscriber
    {
        public long ChatId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsSubscribed { get; set; } = true;
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastNotifiedAt { get; set; }
    }
}