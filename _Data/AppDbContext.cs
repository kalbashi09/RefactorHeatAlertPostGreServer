using Microsoft.EntityFrameworkCore;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options) 
        {
        }

        public DbSet<Sensor> Sensors { get; set; }
        public DbSet<HeatLog> HeatLogs { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Automatically set UpdatedAt for entities that have it
            foreach (var entry in ChangeTracker.Entries<Sensor>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}