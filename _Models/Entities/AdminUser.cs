

namespace RefactorHeatAlertPostGre.Models.Entities
{
    public class AdminUser
    {
        public int AdminUID { get; set; }
        public string PersonnelId { get; set; } = string.Empty;
        public string PasscodeHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
    }
}