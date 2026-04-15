using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Configurations
{
    public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
    {
        public void Configure(EntityTypeBuilder<AdminUser> builder)
        {
            builder.ToTable("auth_personnel");

            builder.HasKey(e => e.AdminUID);

            builder.Property(e => e.AdminUID)
                .HasColumnName("admin_uid")
                .UseIdentityColumn();

            builder.Property(e => e.PersonnelId)
                .HasColumnName("personnel_id")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.PasscodeHash)
                .HasColumnName("passcode_hash")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.FullName)
                .HasColumnName("full_name")
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(e => e.LastLogin)
                .HasColumnName("last_login")
                .IsRequired(false);

            // Unique index on PersonnelId
            builder.HasIndex(e => e.PersonnelId)
                .HasDatabaseName("idx_auth_personnel_personnel_id")
                .IsUnique();

            // Index for active admins query
            builder.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_auth_personnel_is_active");
        }
    }
}