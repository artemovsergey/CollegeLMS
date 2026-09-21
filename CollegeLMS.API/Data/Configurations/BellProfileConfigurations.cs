using CollegeLMS.API.Entities;
using CollegeLMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class BellProfileConfiguration : IEntityTypeConfiguration<BellProfile>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BellProfile> builder)
    {
        builder.ToTable("bell_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.DaysOfWeek).HasColumnType("integer[]");

        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ix_bell_profiles_name");

        builder
            .HasMany(x => x.Slots)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.BigBreaks)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.Dates)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new BellProfile
            {
                Id = BellSeedIds.DefaultProfile,
                Name = "Обычный",
                IsDefault = true,
                DaysOfWeek = [],
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellProfile
            {
                Id = BellSeedIds.MondayProfile,
                Name = "Понедельник",
                IsDefault = false,
                DaysOfWeek = [1],
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellProfile
            {
                Id = BellSeedIds.ThursdayProfile,
                Name = "Четверг",
                IsDefault = false,
                DaysOfWeek = [4],
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }
}

public class BellProfileDateConfiguration : IEntityTypeConfiguration<BellProfileDate>
{
    public void Configure(EntityTypeBuilder<BellProfileDate> builder)
    {
        builder.ToTable("bell_profile_dates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DateFrom).IsRequired();
        builder.Property(x => x.DateTo).IsRequired();

        builder
            .HasOne(x => x.Profile)
            .WithMany(x => x.Dates)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DateFrom).HasDatabaseName("ix_bell_profile_dates_date_from");
        builder.HasIndex(x => x.DateTo).HasDatabaseName("ix_bell_profile_dates_date_to");
    }
}

public class WorkingDayOverrideConfiguration : IEntityTypeConfiguration<WorkingDayOverride>
{
    public void Configure(EntityTypeBuilder<WorkingDayOverride> builder)
    {
        builder.ToTable("working_day_overrides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.DateFrom).IsRequired();
        builder.Property(x => x.DateTo).IsRequired();
        builder.Property(x => x.SubstituteDayOfWeek);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.DateFrom).HasDatabaseName("ix_working_day_overrides_date_from");
        builder.HasIndex(x => x.DateTo).HasDatabaseName("ix_working_day_overrides_date_to");
    }
}

/// <summary>Стабильные идентификаторы seed-профилей звонков (используются в конфигурациях и миграции).</summary>
internal static class BellSeedIds
{
    public static readonly Guid DefaultProfile = new("b3000000-0000-0000-0000-000000000001");
    public static readonly Guid MondayProfile = new("b3000000-0000-0000-0000-000000000002");
    public static readonly Guid ThursdayProfile = new("b3000000-0000-0000-0000-000000000003");

    public static (TimeSpan Start, TimeSpan End) PairTime(DayOfWeek day, int index) =>
        ScheduleImportService.PairTimeSlots[day][index];
}
