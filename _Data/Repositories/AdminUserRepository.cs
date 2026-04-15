using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Repositories
{
    public class AdminUserRepository : IAdminUserRepository
    {
        private readonly AppDbContext _context;

        public AdminUserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminUser?> GetByPersonnelIdAsync(string personnelId, CancellationToken cancellationToken = default)
        {
            return await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.PersonnelId == personnelId && a.IsActive, cancellationToken);
        }

        public async Task<AdminUser?> GetByIdAsync(int adminUid, CancellationToken cancellationToken = default)
        {
            return await _context.AdminUsers
                .FirstOrDefaultAsync(a => a.AdminUID == adminUid && a.IsActive, cancellationToken);
        }

        public async Task<bool> ValidateCredentialsAsync(string personnelId, string passcode, CancellationToken cancellationToken = default)
        {
            var admin = await GetByPersonnelIdAsync(personnelId, cancellationToken);
            
            if (admin == null) return false;
            
            return BCrypt.Net.BCrypt.Verify(passcode, admin.PasscodeHash);
        }

        public async Task UpdateLastLoginAsync(int adminUid, CancellationToken cancellationToken = default)
        {
            var admin = await GetByIdAsync(adminUid, cancellationToken);
            if (admin != null)
            {
                admin.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}