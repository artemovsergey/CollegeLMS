using CollegeLMS.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CollegeLMS.API.Data.Configurations;

public class BellSlotConfiguration : IEntityTypeConfiguration<BellSlot>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BellSlot> builder)
    {
        builder.ToTable("bell_slots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.NumberPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder
            .HasOne(x => x.Profile)
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.ProfileId, x.NumberPair })
            .IsUnique()
            .HasDatabaseName("ix_bell_slots_profile_pair");

        builder.HasData(
            // Базовый (default) профиль: 8 пар 08:30–20:55
            new BellSlot
            {
                Id = SlotId(1, 1),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 1,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(9, 50, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 2),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 2,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 20, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 3),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 3,
                StartTime = new TimeSpan(11, 30, 0),
                EndTime = new TimeSpan(12, 50, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 4),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 4,
                StartTime = new TimeSpan(13, 0, 0),
                EndTime = new TimeSpan(14, 20, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 5),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 5,
                StartTime = new TimeSpan(15, 5, 0),
                EndTime = new TimeSpan(16, 25, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 6),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 6,
                StartTime = new TimeSpan(16, 35, 0),
                EndTime = new TimeSpan(17, 55, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 7),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 7,
                StartTime = new TimeSpan(18, 5, 0),
                EndTime = new TimeSpan(19, 25, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(1, 8),
                ProfileId = BellSeedIds.DefaultProfile,
                NumberPair = 8,
                StartTime = new TimeSpan(19, 35, 0),
                EndTime = new TimeSpan(20, 55, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            // Профиль «Понедельник»: 6 пар с 09:10
            new BellSlot
            {
                Id = SlotId(2, 1),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 1,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 0).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 0).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(2, 2),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 2,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 1).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 1).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(2, 3),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 3,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 2).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 2).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(2, 4),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 4,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 3).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 3).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(2, 5),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 5,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 4).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 4).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(2, 6),
                ProfileId = BellSeedIds.MondayProfile,
                NumberPair = 6,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Monday, 5).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Monday, 5).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            // Профиль «Четверг»: 6 пар
            new BellSlot
            {
                Id = SlotId(3, 1),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 1,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 0).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 0).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(3, 2),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 2,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 1).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 1).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(3, 3),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 3,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 2).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 2).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(3, 4),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 4,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 3).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 3).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(3, 5),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 5,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 4).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 4).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            },
            new BellSlot
            {
                Id = SlotId(3, 6),
                ProfileId = BellSeedIds.ThursdayProfile,
                NumberPair = 6,
                StartTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 5).Start,
                EndTime = BellSeedIds.PairTime(DayOfWeek.Thursday, 5).End,
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }

    private static Guid SlotId(int profile, int number) =>
        new($"b1000000-0000-0000-000{profile}-0000000000{number:00}");
}

public class BigBreakConfiguration : IEntityTypeConfiguration<BigBreak>
{
    private static readonly DateTime SeedDate = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<BigBreak> builder)
    {
        builder.ToTable("big_breaks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.AfterPair).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();

        builder
            .HasOne(x => x.Profile)
            .WithMany(x => x.BigBreaks)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new BigBreak
            {
                Id = new Guid("b2000000-0000-0000-0000-000000000001"),
                ProfileId = BellSeedIds.DefaultProfile,
                AfterPair = 4,
                StartTime = new TimeSpan(14, 20, 0),
                EndTime = new TimeSpan(15, 5, 0),
                CreatedAt = SeedDate,
                UpdatedAt = SeedDate,
            }
        );
    }
}
