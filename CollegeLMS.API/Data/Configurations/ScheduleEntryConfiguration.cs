using CollegeLMS.API.Entities;
using CollegeLMS.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class ScheduleEntryConfiguration : IEntityTypeConfiguration<ScheduleEntry>
{
    public void Configure(EntityTypeBuilder<ScheduleEntry> builder)
    {
        builder.ToTable("schedule_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Room).HasMaxLength(50);
        builder.Property(x => x.NumberPair).IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("interval");
        builder.Property(x => x.EndTime).HasColumnType("interval");
        builder.Property(x => x.Weeks).HasColumnType("integer[]");
        builder.Property(x => x.LessonType).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(x => x.GroupId).HasDatabaseName("ix_schedule_entries_group_id");
        builder.HasIndex(x => x.TeacherId).HasDatabaseName("ix_schedule_entries_teacher_id");
        builder.HasIndex(x => x.Room).HasDatabaseName("ix_schedule_entries_room");
        builder.HasIndex(x => x.NumberPair).HasDatabaseName("ix_schedule_entries_number_pair");

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
