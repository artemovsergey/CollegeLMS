using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class ScheduleInsertConfiguration : IEntityTypeConfiguration<ScheduleInsert>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ScheduleInsert> builder)
    {
        builder.ToTable("schedule_inserts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.DayOfWeek).HasDatabaseName("ix_schedule_inserts_day_of_week");

        builder.HasData(
            new ScheduleInsert
            {
                Id = new Guid("b3000000-0000-0000-0000-000000000001"),
                Title = "Разговор о важном",
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(9, 0, 0),
                Course = null,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new ScheduleInsert
            {
                Id = new Guid("b3000000-0000-0000-0000-000000000002"),
                Title = "Классный час",
                DayOfWeek = DayOfWeek.Thursday,
                StartTime = new TimeSpan(12, 10, 0),
                EndTime = new TimeSpan(13, 0, 0),
                Course = null,
                IsActive = true,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }
}
