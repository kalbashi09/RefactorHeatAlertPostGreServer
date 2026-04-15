using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Configurations
{
    public class HeatLogConfiguration : IEntityTypeConfiguration<HeatLog>
    {
        public void Configure(EntityTypeBuilder<HeatLog> builder)
        {
            builder.ToTable("heat_logs");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            builder.Property(e => e.SensorId)
                .HasColumnName("sensor_id")
                .IsRequired();

            builder.Property(e => e.RecordedTemp)
                .HasColumnName("recorded_temp")
                .IsRequired();

            builder.Property(e => e.HeatIndex)
                .HasColumnName("heat_index")
                .IsRequired();

            builder.Property(e => e.RecordedAt)
                .HasColumnName("recorded_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // Foreign Key relationship
            builder.HasOne(e => e.Sensor)
                .WithMany(s => s.HeatLogs)
                .HasForeignKey(e => e.SensorId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_heat_logs_sensor");

            // Indexes for performance
            builder.HasIndex(e => e.SensorId)
                .HasDatabaseName("idx_heat_logs_sensor_id");

            builder.HasIndex(e => e.RecordedAt)
                .HasDatabaseName("idx_heat_logs_recorded_at")
                .IsDescending();

            builder.HasIndex(e => e.HeatIndex)
                .HasDatabaseName("idx_heat_logs_heat_index");
        }
    }
}