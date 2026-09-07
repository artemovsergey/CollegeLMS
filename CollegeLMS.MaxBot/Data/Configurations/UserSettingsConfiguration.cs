using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.MaxBot.Data.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MaxUserId).HasColumnName("max_user_id");

        builder.Property(x => x.MaxChatId).HasColumnName("max_chat_id");

        builder.Property(x => x.Role).HasColumnName("role").HasMaxLength(20);

        builder.Property(x => x.GroupId).HasColumnName("group_id");

        builder.Property(x => x.TeacherId).HasColumnName("teacher_id");

        builder.Property(x => x.NotifyEnabled).HasColumnName("notify_enabled");

        builder.Property(x => x.NotifyDays).HasColumnName("notify_days");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        builder
            .HasIndex(x => x.MaxUserId)
            .IsUnique()
            .HasDatabaseName("ix_user_settings_max_user_id");
    }
}
