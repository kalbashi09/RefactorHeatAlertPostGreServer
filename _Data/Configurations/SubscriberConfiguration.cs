using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RefactorHeatAlertPostGre.Models.Entities;

namespace RefactorHeatAlertPostGre.Data.Configurations
{
    public class SubscriberConfiguration : IEntityTypeConfiguration<Subscriber>
    {
        public void Configure(EntityTypeBuilder<Subscriber> builder)
        {
            builder.ToTable("subscribers");

            builder.HasKey(e => e.ChatId);

            builder.Property(e => e.ChatId)
                .HasColumnName("chat_id")
                .ValueGeneratedNever(); // ChatId is provided by Telegram

            builder.Property(e => e.Username)
                .HasColumnName("username")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.IsSubscribed)
                .HasColumnName("is_subscribed")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(e => e.SubscribedAt)
                .HasColumnName("subscribed_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(e => e.LastNotifiedAt)
                .HasColumnName("last_notified_at")
                .IsRequired(false);

            // Index for active subscribers query
            builder.HasIndex(e => e.IsSubscribed)
                .HasDatabaseName("idx_subscribers_is_subscribed");
        }
    }
}