using CollegeLMS.MaxBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.MaxBot.Data.Configurations;

public class ScheduleRevisionConfiguration : IEntityTypeConfiguration<ScheduleRevision>
{
    public void Configure(EntityTypeBuilder<ScheduleRevision> builder)
    {
        builder.ToTable("schedule_revisions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ForeignId).HasColumnName("foreign_id");

        builder.Property(x => x.ChangeType).HasColumnName("change_type").HasMaxLength(20);

        builder.Property(x => x.GroupName).HasColumnName("group_name").HasMaxLength(200);

        builder.Property(x => x.TeacherName).HasColumnName("teacher_name").HasMaxLength(200);

        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(300);

        builder.Property(x => x.Room).HasColumnName("room").HasMaxLength(100);

        builder.Property(x => x.DayOfWeek).HasColumnName("day_of_week").HasMaxLength(20);

        builder.Property(x => x.Week).HasColumnName("week");

        builder.Property(x => x.NumberPair).HasColumnName("number_pair");

        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);

        builder.Property(x => x.RemovedSubject).HasColumnName("removed_subject").HasMaxLength(300);

        builder
            .Property(x => x.RemovedTeacherName)
            .HasColumnName("removed_teacher_name")
            .HasMaxLength(200);

        builder.Property(x => x.RemovedNumberPair).HasColumnName("removed_number_pair");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder
            .HasIndex(x => x.GroupName)
            .HasDatabaseName("ix_schedule_revisions_group_name");

        builder
            .HasIndex(x => x.TeacherName)
            .HasDatabaseName("ix_schedule_revisions_teacher_name");

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_schedule_revisions_created_at");
    }
}