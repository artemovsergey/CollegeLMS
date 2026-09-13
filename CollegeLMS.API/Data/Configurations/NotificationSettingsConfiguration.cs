using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class NotificationSettingsConfiguration : IEntityTypeConfiguration<NotificationSettings>
{
    public void Configure(EntityTypeBuilder<NotificationSettings> builder)
    {
        builder.ToTable("notification_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Time).HasColumnType("interval");
        builder.Property(x => x.Days).HasColumnType("integer[]");

        builder
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("ux_notification_settings_user_id");
    }
}
