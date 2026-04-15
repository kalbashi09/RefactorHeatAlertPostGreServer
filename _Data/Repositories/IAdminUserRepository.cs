using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public interface IAdminUserRepository
    {
        Task<AdminUser?> GetByPersonnelIdAsync(string personnelId, CancellationToken cancellationToken = default);
        Task<AdminUser?> GetByIdAsync(int adminUid, CancellationToken cancellationToken = default);
        Task<bool> ValidateCredentialsAsync(string personnelId, string passcode, CancellationToken cancellationToken = default);
        Task UpdateLastLoginAsync(int adminUid, CancellationToken cancellationToken = default);
    }
}