using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Configurations
{
    public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
    {
        public void Configure(EntityTypeBuilder<Sensor> builder)
        {
            builder.ToTable("sensor_registry");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            builder.Property(e => e.SensorCode)
                .HasColumnName("sensor_code")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.DisplayName)
                .HasColumnName("display_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Barangay)
                .HasColumnName("barangay")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .HasColumnType("decimal(9,6)")
                .IsRequired();

            builder.Property(e => e.BaselineTemp)
                .HasColumnName("baseline_temp")
                .HasDefaultValue(30)
                .IsRequired();

            builder.Property(e => e.EnvironmentType)
                .HasColumnName("environment_type")
                .HasMaxLength(50)
                .HasDefaultValue("Unknown");

            builder.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired(false);

            // Unique index on SensorCode
            builder.HasIndex(e => e.SensorCode)
                .HasDatabaseName("idx_sensor_registry_sensor_code")
                .IsUnique();

            // Index for active sensors query
            builder.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_sensor_registry_is_active");

            // Index on Barangay for filtering
            builder.HasIndex(e => e.Barangay)
                .HasDatabaseName("idx_sensor_registry_barangay");
        }
    }
}