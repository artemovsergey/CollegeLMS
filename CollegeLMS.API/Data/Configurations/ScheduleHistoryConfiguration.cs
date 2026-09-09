using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class ScheduleHistoryConfiguration : IEntityTypeConfiguration<ScheduleHistory>
{
    public void Configure(EntityTypeBuilder<ScheduleHistory> builder)
    {
        builder.ToTable("schedule_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Room).HasMaxLength(50);
        builder.Property(x => x.Note).HasMaxLength(200);
        builder.Property(x => x.RemovedSubject).HasMaxLength(200);
        builder.Property(x => x.RemovedRoom).HasMaxLength(50);
        builder.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(x => x.GroupId).HasDatabaseName("ix_schedule_history_group_id");
        builder.HasIndex(x => x.DayOfWeek).HasDatabaseName("ix_schedule_history_day_of_week");
        builder.HasIndex(x => x.AppliedAt).HasDatabaseName("ix_schedule_history_applied_at");

        builder
            .HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}