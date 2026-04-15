

namespace RefactorHeatAlertPostGre.Models.Dto
{
    public class LoginRequest
    {
        public string PersonnelId { get; set; } = string.Empty;
        public string Passcode { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Token { get; set; }
    }

    public class SubscriberRequest
    {
        public long ChatId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}