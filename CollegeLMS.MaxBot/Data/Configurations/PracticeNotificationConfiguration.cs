using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.MaxBot.Data.Configurations;

public class PracticeNotificationConfiguration : IEntityTypeConfiguration<PracticeNotification>
{
    public void Configure(EntityTypeBuilder<PracticeNotification> builder)
    {
        builder.ToTable("practice_notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.PracticeId).HasColumnName("practice_id");

        builder
            .Property(x => x.Event)
            .HasColumnName("event")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.SentOn).HasColumnName("sent_on").HasColumnType("date");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        // Уникальность (practice_id, event): одна отправка на событие практики,
        // повторная попытка (рестарт/вторая реплика) просто пропускается.
        builder
            .HasIndex(x => new { x.PracticeId, x.Event })
            .IsUnique()
            .HasDatabaseName("ix_practice_notifications_practice_event");
    }
}
