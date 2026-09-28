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

/// <summary>
/// Стабильные идентификаторы seed-профилей звонков и сами сиды (используются
/// в конфигурациях EF и проверяются тестами).
/// </summary>
internal static class BellSeedIds
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Guid DefaultProfile = new("b3000000-0000-0000-0000-000000000001");
    public static readonly Guid MondayProfile = new("b3000000-0000-0000-0000-000000000002");
    public static readonly Guid ThursdayProfile = new("b3000000-0000-0000-0000-000000000003");

    public static (TimeSpan Start, TimeSpan End) PairTime(DayOfWeek day, int index) =>
        ScheduleImportService.PairTimeSlots[day][index];

    private static Guid SlotId(int profile, int number) =>
        new($"b1000000-0000-0000-000{profile}-0000000000{number:00}");

    private static BellSlot Slot(
        Guid profileId,
        int profileNumber,
        int numberPair,
        DayOfWeek day,
        int index
    ) =>
        new()
        {
            Id = SlotId(profileNumber, numberPair),
            ProfileId = profileId,
            NumberPair = numberPair,
            StartTime = PairTime(day, index).Start,
            EndTime = PairTime(day, index).End,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate,
        };

    private static BellSlot Slot(
        Guid profileId,
        int profileNumber,
        int numberPair,
        TimeSpan start,
        TimeSpan end
    ) =>
        new()
        {
            Id = SlotId(profileNumber, numberPair),
            ProfileId = profileId,
            NumberPair = numberPair,
            StartTime = start,
            EndTime = end,
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate,
        };

    /// <summary>
    /// Пары по профилям: дефолтный — расписание вторника/среды/пятницы (7 пар),
    /// понедельник и четверг — свои (по 6 пар). Время берётся из справочника пар
    /// импорта, чтобы сид и код не расходились.
    /// </summary>
    public static IReadOnlyList<BellSlot> SeededSlots { get; } =
    [
        .. Enumerable.Range(1, 7).Select(n => Slot(DefaultProfile, 1, n, DayOfWeek.Tuesday, n - 1)),
        .. Enumerable.Range(1, 6).Select(n => Slot(MondayProfile, 2, n, DayOfWeek.Monday, n - 1)),
        .. Enumerable
            .Range(1, 6)
            .Select(n => Slot(ThursdayProfile, 3, n, DayOfWeek.Thursday, n - 1)),
    ];

    /// <summary>
    /// Большие перемены после 2-й пары: вт/ср/пт 11:40–12:10, в понедельник
    /// 12:35–13:05. В четверг на этом месте организационный/классный час 12:10–12:55,
    /// поэтому большой перемены там нет.
    /// </summary>
    public static IReadOnlyList<BigBreak> SeededBigBreaks { get; } =
    [
        new()
        {
            Id = new Guid("b2000000-0000-0000-0000-000000000001"),
            ProfileId = DefaultProfile,
            AfterPair = 2,
            StartTime = new TimeSpan(11, 40, 0),
            EndTime = new TimeSpan(12, 10, 0),
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate,
        },
        new()
        {
            Id = new Guid("b2000000-0000-0000-0000-000000000002"),
            ProfileId = MondayProfile,
            AfterPair = 2,
            StartTime = new TimeSpan(12, 35, 0),
            EndTime = new TimeSpan(13, 5, 0),
            CreatedAt = SeedDate,
            UpdatedAt = SeedDate,
        },
    ];
}
